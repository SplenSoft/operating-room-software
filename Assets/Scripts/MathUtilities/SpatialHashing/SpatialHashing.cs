﻿using System;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections;

public class SpatialHashing : MonoBehaviour {
  public bool useSpatialHash = false, useEnumerator = false;
  public int numParticles = 10000;
  [Range(-1f, 1f)]
  public float floorHeight = 0.001f;
  [Range(0.25f, 10f)]
  public float sphereRadius = 2.5f;
  [Range(0.01f, 0.5f)]
  public float particleDiameter = 0.2f;
  public Mesh sphereMesh;
  public Material sphereMaterial;

  SpatialHashData data;
  Matrix4x4[] particles;
  Matrix4x4[] instanceMatrices = new Matrix4x4[1023];

  public struct SpatialHashData : IDisposable {
    public NativeArray<Vector3> particles, prevParticles, offsets;
    public NativeArray<Matrix4x4> particleMatrices;
    public SpatialHash hash;

    public SpatialHashData(int numParticles, float floorHeight, float particleDiameter) {
      particles        = new NativeArray<Vector3  >(numParticles, Allocator.Persistent);
      prevParticles    = new NativeArray<Vector3  >(numParticles, Allocator.Persistent);
      offsets          = new NativeArray<Vector3  >(numParticles, Allocator.Persistent);
      particleMatrices = new NativeArray<Matrix4x4>(numParticles, Allocator.Persistent);
      hash             = new SpatialHash(particleDiameter, numParticles);
      for (int i = 0; i < particles.Length; i++) {
        particles[i] = UnityEngine.Random.insideUnitSphere * 5f;
        particles[i] = new Vector3(particles[i].x, Mathf.Abs(particles[i].y - floorHeight) + floorHeight, particles[i].z);
        prevParticles[i] = particles[i];
        offsets[i] = Vector3.zero;
        particleMatrices[i] = Matrix4x4.identity;
      }
      hash.create(particles, particleDiameter);
    }

    [Unity.Burst.BurstCompile]
    public struct Integrate : IJobParallelFor {
      public NativeArray<Vector3> particles, prevParticles;
      public void Execute(int i) {
        // Apply Verlet
        Vector3 tempPos = particles[i];
        particles[i] += (particles[i] - prevParticles[i]) + (Vector3.down * 0.001f); ;// * 0.95f;
        prevParticles[i] = tempPos;
      }
    }

    [Unity.Burst.BurstCompile]
    public struct CalculateCollisionOffsetsNaive : IJobParallelFor {
      public NativeArray<Vector3> offsets;
      [ReadOnly]
      public NativeArray<Vector3> particles;
      [ReadOnly]
      public float particleDiameter;

      public void Execute(int i) {
        // Collide Particles with each other
        offsets[i] = Vector3.zero; int samples = 0;
        for (int j = 0; j < particles.Length; j++) {
          if (i != j) {
            Vector3 offset = particles[i] - particles[j];
            float offsetMagnitude = offset.magnitude;
            if (offsetMagnitude < particleDiameter) {
              Vector3 normalizedOffset = offset / offsetMagnitude;
              offsets[i] += normalizedOffset * (particleDiameter - offsetMagnitude) * 0.5f;
              samples++;
            }
          }
        }
        if (samples > 0) { offsets[i] /= samples; }
      }
    }

    [Unity.Burst.BurstCompile]
    public struct ConstructSpatialHash : IJob {
      public SpatialHash hash;
      [ReadOnly]
      public NativeArray<Vector3> particles;
      [ReadOnly]
      public float particleDiameter;

      public void Execute() {
        hash.create(particles, particleDiameter);
      }
    }
    [Unity.Burst.BurstCompile]
    public struct CalculateCollisionOffsetsHash : IJobParallelFor {
      //public SpatialHash hash;
      public NativeArray<Vector3> offsets;
      [ReadOnly]
      public NativeArray<Vector3> particles;
      // Need to specify these separately because they're ReadOnly here, but writable in another function
      [ReadOnly]
      public float particleDiameter, spacing;
      [ReadOnly]
      public int tableSize;
      [ReadOnly]
      public NativeArray<int> cellStart, cellEntries;

      public void Execute(int i) {
        // Query the spatial hash
        int x0 = SpatialHash.intCoord(particles[i].x - particleDiameter, spacing);
        int y0 = SpatialHash.intCoord(particles[i].y - particleDiameter, spacing);
        int z0 = SpatialHash.intCoord(particles[i].z - particleDiameter, spacing);
        int x1 = SpatialHash.intCoord(particles[i].x + particleDiameter, spacing);
        int y1 = SpatialHash.intCoord(particles[i].y + particleDiameter, spacing);
        int z1 = SpatialHash.intCoord(particles[i].z + particleDiameter, spacing);

        offsets[i] = Vector3.zero; int samples = 0;
        for (int xi = x0; xi <= x1; xi++) {
          for (int yi = y0; yi <= y1; yi++) {
            for (int zi = z0; zi <= z1; zi++) {
              int h     = SpatialHash.hashCoords(xi, yi, zi, tableSize);
              int start = cellStart[h];
              int end   = cellStart[h + 1];

              for (int q = start; q < end; q++) {
                int j = cellEntries[q];
                // ------ Ideally this code could be wrapped inside of the hash's query iterator ------
                if (i != j) {
                  // Collide these two particles together
                  Vector3 offset = particles[i] - particles[j];
                  float offsetMagnitude = offset.magnitude;
                  if (offsetMagnitude < particleDiameter) {
                    Vector3 normalizedOffset = offset / offsetMagnitude;
                    offsets[i] += normalizedOffset * (particleDiameter - offsetMagnitude) * 0.5f;
                    samples++;
                  }
                }
                // ------------------------------------------------------------------------------------
              }
            }
          }
        }
        if (samples > 0) { offsets[i] /= samples; }
      }
    }
    [Unity.Burst.BurstCompile]
    public struct CalculateCollisionOffsetsHashEnumerator : IJobParallelFor {
      [ReadOnly]
      public SpatialHash hash;
      public NativeArray<Vector3> offsets;
      [ReadOnly]
      public NativeArray<Vector3> particles;
      // Need to specify these separately because they're ReadOnly here, but writable in another function
      [ReadOnly]
      public float particleDiameter, spacing;

      public void Execute(int i) {
        offsets[i] = Vector3.zero; int samples = 0;
        foreach (int j in hash.QueryPosition(particles[i], particleDiameter)) {
          if (i != j) {
            // Collide these two particles together
            Vector3 offset = particles[i] - particles[j];
            float offsetMagnitude = offset.magnitude;
            if (offsetMagnitude < particleDiameter) {
              Vector3 normalizedOffset = offset / offsetMagnitude;
              offsets[i] += normalizedOffset * (particleDiameter - offsetMagnitude) * 0.5f;
              samples++;
            }
          }
        }
        if (samples > 0) { offsets[i] /= samples; }
      }
    }

    [Unity.Burst.BurstCompile]
    public struct ApplyCollisionOffsets : IJobParallelFor {
      public NativeArray<Vector3> particles, offsets;
      public NativeArray<Matrix4x4> particleMatrices;
      public float floorHeight, particleDiameter, sphereRadius;

      public void Execute(int i) {
        particles[i] += offsets[i];

        // Bound to Ground
        particles[i] = new Vector3(particles[i].x, Mathf.Max(floorHeight, particles[i].y), particles[i].z);

        // Bound to Sphere
        if (particles[i].magnitude > sphereRadius) { particles[i] = particles[i].normalized * sphereRadius; }

        particleMatrices[i] = Matrix4x4.TRS(particles[i], new Quaternion(0f, 0f, 0f, 1f), new Vector3(1f, 1f, 1f) * particleDiameter * 0.5f);
      }
    }

    public void Dispose() {
      particles.Dispose();
      prevParticles.Dispose();
      offsets.Dispose();
      particleMatrices.Dispose();
      hash.Dispose();
    }
  }

  public void Update() {
    if (!data.particles.IsCreated || data.particles.Length != numParticles) {
      if (data.particles.IsCreated) { data.Dispose(); }
      data = new SpatialHashData(numParticles, floorHeight, particleDiameter);
    }

    // Integrate, Collide, and Apply
    JobHandle previousHandle = new SpatialHashData.Integrate() {
      particles = data.particles, prevParticles = data.prevParticles
    }.Schedule(data.particles.Length, 32);
    if (useSpatialHash) {
      previousHandle = new SpatialHashData.ConstructSpatialHash() {
        hash = data.hash, particles = data.particles, particleDiameter = particleDiameter
      }.Schedule(dependsOn: previousHandle);


      if (useEnumerator) {
        previousHandle = new SpatialHashData.CalculateCollisionOffsetsHashEnumerator() {
          hash = data.hash, particles = data.particles, offsets = data.offsets, 
          particleDiameter = particleDiameter, spacing = particleDiameter
        }.Schedule(data.particles.Length, 32, dependsOn: previousHandle);
      } else {
        previousHandle = new SpatialHashData.CalculateCollisionOffsetsHash() {
          particles = data.particles, offsets = data.offsets, particleDiameter = particleDiameter,
          cellStart = data.hash.cellStart, cellEntries = data.hash.cellEntries, spacing = particleDiameter, tableSize = data.hash.tableSize
        }.Schedule(data.particles.Length, 32, dependsOn: previousHandle);
      }
    } else {
      // Calculate the n^2 collisions
      previousHandle = new SpatialHashData.CalculateCollisionOffsetsNaive() {
        particles = data.particles, offsets = data.offsets, particleDiameter = particleDiameter
      }.Schedule(data.particles.Length, 32, dependsOn: previousHandle);
    }
    previousHandle = new SpatialHashData.ApplyCollisionOffsets() {
      particles = data.particles, particleMatrices = data.particleMatrices,
      offsets = data.offsets, floorHeight = floorHeight, particleDiameter = particleDiameter, sphereRadius = sphereRadius
    }.Schedule(data.particles.Length, 32, dependsOn: previousHandle);
    previousHandle.Complete();

    // Draw the particles 1023 at a time
    data.particleMatrices.CopyTo(particles);
    int particlesDrawn = 0;
    while (particlesDrawn < numParticles) {
      int particlesToDraw = Mathf.Min(1023, numParticles - particlesDrawn);
      System.Array.Copy(particles, particlesDrawn, instanceMatrices, 0, particlesToDraw);
      Graphics.DrawMeshInstanced(sphereMesh, 0, sphereMaterial, instanceMatrices, particlesToDraw, null, UnityEngine.Rendering.ShadowCastingMode.Off);
      particlesDrawn += particlesToDraw;
    }

  }

  public void Start() { if (data.particles.IsCreated) { data.Dispose(); } particles = new Matrix4x4[numParticles]; }
  public void OnDestroy() { data.Dispose(); }
}


public struct SpatialHash : IEnumerable {
  private Vector3 _pos;
  public float spacing, maxDist; public int tableSize;
  public NativeArray<int> cellStart, cellEntries;
  public SpatialHash(float spacing, int maxNumObjects) {
    this.spacing = spacing;
    tableSize    = 2 * maxNumObjects;
    cellStart    = new NativeArray<int>(tableSize + 1, Allocator.Persistent);
    cellEntries  = new NativeArray<int>(maxNumObjects, Allocator.Persistent);
    _pos         = Vector3.zero;
    maxDist      = spacing;
  }

  // Static Functions
  public static int hashCoords(int xi, int yi, int zi, int tableSize) {
    var h = (xi * 92837111) ^ (yi * 689287499) ^ (zi * 283923481);  // fantasy function
    return Mathf.Abs(h) % tableSize;
  }
  public static int intCoord(float coord, float spacing) {
    return (int)Mathf.Floor(coord / spacing);
  }
  public static int hashPos(Vector3 pos, float spacing, int tableSize) {
    return hashCoords(
      intCoord(pos.x, spacing),
      intCoord(pos.y, spacing),
      intCoord(pos.z, spacing), tableSize);
  }

  public void create(NativeArray<Vector3> pos, float spacing) {
    int numObjects = Mathf.Min(pos.Length, cellEntries.Length);

    // 1. Determine cell sizes
    for (int i = 0; i <   cellStart.Length; i++) { cellStart  [i] = 0; }
    for (int i = 0; i < cellEntries.Length; i++) { cellEntries[i] = 0; }
    for (int i = 0; i <         numObjects; i++) { cellStart[hashPos(pos[i], spacing, tableSize)]++; }

    // 2. Determine cells starts
    int start = 0;
    for (int i = 0; i < tableSize; i++) {
      start += cellStart[i];
      cellStart[i] = start;
    }
    cellStart[tableSize] = start; // Guard

    // 3. Fill in objects ids
    for (var i = 0; i < numObjects; i++) {
      int h = hashPos(pos[i], spacing, tableSize);
      cellStart[h]--;
      cellEntries[cellStart[h]] = i;
    }
  }

  public SpatialHash QueryPosition(Vector3 pos, float maxDist) { _pos = pos; this.maxDist = maxDist; return this; }
  IEnumerator IEnumerable.GetEnumerator() { return (IEnumerator)GetEnumerator(); }
  public SpatialHashEnumerator GetEnumerator() {
    return new SpatialHashEnumerator(_pos, maxDist, spacing, tableSize, cellStart, cellEntries);
  }

  public void Dispose() {
    cellStart.Dispose(); cellEntries.Dispose();
  }
}

public struct SpatialHashEnumerator : IEnumerator {
  public Vector3 pos; public Vector3Int corner1, curCell; public int position, end;
  public float spacing, maxDist; public int tableSize;
  [ReadOnly]
  public NativeArray<int> cellStart, cellEntries;

  public SpatialHashEnumerator(Vector3 pos, float maxDist, float spacing, 
    int tableSize, NativeArray<int> cellStart, NativeArray<int> cellEntries) {
    this.pos         = pos;
    this.maxDist     = maxDist;
    this.spacing     = spacing;
    this.tableSize   = tableSize;
    this.cellStart   = cellStart;
    this.cellEntries = cellEntries;

    this.curCell = new Vector3Int(SpatialHash.intCoord(pos.x - maxDist, spacing),
                                  SpatialHash.intCoord(pos.y - maxDist, spacing),
                                  SpatialHash.intCoord(pos.z - maxDist, spacing));
    this.corner1 = new Vector3Int(SpatialHash.intCoord(pos.x + maxDist, spacing),
                                  SpatialHash.intCoord(pos.y + maxDist, spacing),
                                  SpatialHash.intCoord(pos.z + maxDist, spacing));
    int h = SpatialHash.hashCoords(curCell.x, curCell.y, curCell.z, tableSize);
    position = cellStart[h] - 1; // Enumerators are positioned before the first element until the first MoveNext() call.
    end = cellStart[h + 1];
  }

  public int         Current { get { return cellEntries[position]; } }
  object IEnumerator.Current { get { return Current; } }

  public void Reset() {
    this.curCell = new Vector3Int(SpatialHash.intCoord(pos.x - maxDist, spacing),
                                  SpatialHash.intCoord(pos.y - maxDist, spacing),
                                  SpatialHash.intCoord(pos.z - maxDist, spacing));
    int h = SpatialHash.hashCoords(curCell.x, curCell.y, curCell.z, tableSize);
    position = cellStart[h] - 1; // Enumerators are positioned before the first element until the first MoveNext() call.
    end      = cellStart[h + 1];
  }

  public bool MoveNext() {
    if(position < end) {
      position++;
      return true;
    } else if (curCell != corner1 + Vector3Int.one) { // TODO: Scrutinize this line!!!!
             if (curCell.z <= corner1.z) {
        curCell.z++;
      } else if (curCell.y <= corner1.y) {
        curCell.y++;
      } else if (curCell.x <= corner1.x) {
        curCell.x++;
      }
      int h = SpatialHash.hashCoords(curCell.x, curCell.y, curCell.z, tableSize);
      position = cellStart[h];
      end = cellStart[h + 1];
      return true;
    }
    return false;
  }
}
