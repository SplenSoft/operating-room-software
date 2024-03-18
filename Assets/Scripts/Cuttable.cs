using Parabox.CSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Can be attached to wall objects to define that they can be
/// cut but <see cref="WallCutter"/> objects. Uses 
/// <see cref="Parabox.CSG"/> library for mesh subtraction
/// </summary>
public class Cuttable : MonoBehaviour
{
    private static List<Cuttable> ActiveCuttables { get; } = new();

    private Mesh _originalMesh;
    private MeshFilter _filter;
    private MeshRenderer _meshRenderer;
    private Mesh _currentMesh;
    private GizmoHandler _gizmoHandler;
    private Selectable _selectable;
    private Collider _collider;

    private void Awake()
    {
        ActiveCuttables.Add(this);

        _collider = GetComponentInChildren<Collider>();
        _filter = GetComponentInChildren<MeshFilter>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _gizmoHandler = GetComponent<GizmoHandler>();
        _originalMesh = _filter.sharedMesh;
        _selectable = GetComponent<Selectable>();

        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.AddListener(Cut);
        }

        if (_selectable != null)
        {
            _selectable.OnPlaced.AddListener(Cut);
        }
    }

    private void OnDestroy()
    {
        ActiveCuttables.Remove(this);

        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.RemoveListener(Cut);
        }

        if (_selectable != null)
        {
            _selectable.OnPlaced.RemoveListener(Cut);
        }
    }

    private void Start()
    {
        Cut();
    }

    public static void UpdateCuts()
    {
        ActiveCuttables.ForEach(x => x.Cut());
    }

    private void UpdateMaterials()
    {
        var materials = _meshRenderer.sharedMaterials.ToList();

        Material firstMaterial = materials[0];

        while (materials.Count < _filter.sharedMesh.subMeshCount)
        {
            materials.Add(firstMaterial);
        }

        _meshRenderer.sharedMaterials = materials.ToArray();
    }

    public void Cut()
    {
        // Set back to original mesh
        _filter.sharedMesh = _originalMesh;
        ((MeshCollider)_collider).sharedMesh = _filter.sharedMesh;

        // Get all wallcutters in scene
        var wallCutters = Selectable.ActiveSelectables
            .SelectMany(x => x.GetComponentsInChildren<WallCutter>())
            .ToList();

        foreach (var wallCutter in wallCutters)
        {
            // Not sure if this saves any performance, but its
            // probably the most cost-effective way to determine
            // if the wallCutter and target mesh intersect.
            // If we don't do this, we're going to generate and
            // dump more meshes than we need to, so for now I
            // think it's worth it
            bool collides = Physics.ComputePenetration(
                _collider, 
                transform.position, 
                transform.rotation, 
                wallCutter.Collider, 
                wallCutter.CutArea.transform.position, 
                wallCutter.CutArea.transform.rotation, 
                out _, 
                out _);

            if (!collides) continue;

            var result = CSG.Subtract(_filter.gameObject, wallCutter.CutArea);

            var mesh = ((Mesh)result);
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 vert = verts[i];

                vert.x -= _filter.transform.position.x;
                vert.x /= _filter.transform.localScale.x;

                vert.y -= _filter.transform.position.y;
                vert.y /= _filter.transform.localScale.y;

                vert.z -= _filter.transform.position.z;
                vert.z /= _filter.transform.localScale.z;
                
                verts[i] = vert;
            }

            mesh.SetVertices(verts);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            _filter.sharedMesh = mesh;

            if (_currentMesh != null)
            {
                // Stop memory leaks
                Destroy(_currentMesh);
            }

            _currentMesh = mesh;
        }

        ((MeshCollider)_collider).sharedMesh = _filter.sharedMesh;
        UpdateMaterials();
    }
}