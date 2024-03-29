using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using UnityEngine.Events;

/*
    A modification to the UnityObjExporter
    at https://github.com/dsapandora/UnityObjExporter
    and made available under the GPU 3.0 License
    Modified by John "Splen" Shepard of SplenSoft (splensoft.com)
    Copyright (C) 2024 SplenSoft LLC

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    See https://www.gnu.org/licenses/
*/

public static class ObjExporter
{
    /// <summary>
    /// Passes output path as argument. Fires when all tasks
    /// are complete and the data is written to file in the
    /// persistent app data
    /// </summary>
    public static UnityEvent<string> ExportFinishedSuccessfully
    { get; } = new();

    public static UnityEvent OnExportStarted 
    { get; } = new();

    /// <summary>
    /// Fires each time a submesh is written to .obj/.mtl
    /// <see cref="StringBuilder"/>. Passes overall progress
    /// as 0-1 float
    /// </summary>
    public static UnityEvent<float> OnSubMeshProcessed
    { get; } = new();

    /// <summary>
    /// Passes overall combination progress as a 0-1 float
    /// </summary>
    public static UnityEvent<float> OnMeshCombiningUpdate
    { get; } = new();

    public static UnityEvent OnMeshCombineSuccess
    { get; } = new();

    /// <summary>
    /// Fires when all mesh .obj data and .mtl data has been 
    /// written to each <see cref="StringBuilder"/>. The data
    /// has not yet been written to file. See 
    /// <see cref="ExportFinishedSuccessfully"/>
    /// </summary>
    public static UnityEvent OnMeshDataWritten
    { get; } = new();

    /// <summary>
    /// Fires regardless of success or failure
    /// </summary>
    public static UnityEvent OnExportFinished
    { get; } = new();

    /// <summary>
    /// Outputs the result to 
    /// <see cref="Application.persistentDataPath"/> in a 
    /// directory called "obj"
    /// </summary>
    /// <param name="makeSubmeshes"></param>
    /// <param name="meshFilters"></param>
    /// <param name="name"></param>
    public static async void DoExport(
    bool makeSubmeshes,
    MeshFilter[] meshFilters,
    string name)
    {
        ObjExporterScript.Start();

        OnExportStarted?.Invoke();

        try
        {
            string meshName = name;

            Debug.Log($"Found {meshFilters.Length} meshfilters");
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            List<Material> materials = new();
            int i = 0;
            while (i < meshFilters.Length)
            {
                var filter = meshFilters[i];

                if (filter.sharedMesh.vertexCount > 0)
                {
                    var meshRenderer = filter.gameObject.GetComponent<MeshRenderer>();
                    for (int j = 0; j < meshRenderer.sharedMaterials.Length; j++)
                    {
                        CombineInstance instance = new()
                        {
                            mesh = filter.sharedMesh,
                            transform = filter.transform.localToWorldMatrix,
                            subMeshIndex = j
                        };
                        materials.Add(meshRenderer.sharedMaterials[j]);
                        combineInstances.Add(instance);
                    }
                }

                i++;
                OnMeshCombiningUpdate?.Invoke((float)i / (meshFilters.Length + 1));

                await Task.Yield();
                if (!Application.isPlaying)
                    throw new Exception("App quit during task");
            }
            Debug.Log($"Combining {combineInstances.Count} combine instances ...");
            CombineInstance[] combine = combineInstances.ToArray();

            Mesh mesh = new()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            mesh.CombineMeshes(combine, mergeSubMeshes: false, useMatrices: true);
            OnMeshCombineSuccess?.Invoke();

            // Breathing room for applications
            // after the mesh combine. Prevents
            // some apps from closing if there's no
            // response for too long
            await Task.Yield();
            if (!Application.isPlaying)
                throw new Exception("App quit during task");

            ObjExportData data = new ObjExportData();

            data.Obj.Append("#" + meshName + ".obj"
                + "\n#" + System.DateTime.Now.ToLongDateString()
                + "\n#" + System.DateTime.Now.ToLongTimeString()
                + "\n#-------"
                + "\n\n");

            var obj = new GameObject("Mesh", typeof(MeshFilter));
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;

            Transform t = obj.transform;

            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            if (!makeSubmeshes)
            {
                data.Obj.Append("g ").Append(t.name).Append("\n");
            }

            var task = ProcessTransform(t, makeSubmeshes, materials, data);
            await task;
            if (!Application.isPlaying)
                throw new Exception("App quit during task");

            OnMeshDataWritten?.Invoke();

            string id = Guid.NewGuid().ToString();

            data.Obj.Append($"mtllib {id}.mtl");

            data.Bake();

            var path = Path.Combine(Application.persistentDataPath, "obj", id);

            Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, $"{id}.obj"), data.ObjString);
            File.WriteAllText(Path.Combine(path, $"{id}.mtl"), data.MtlString);

            foreach (var item in data.Textures)
            {
                byte[] imageByteArray = Convert.FromBase64String(item.TextureBase64);
                File.WriteAllBytes(Path.Combine(path, $"{item.Name}.png"), imageByteArray);
            }

            ExportFinishedSuccessfully?.Invoke(path);

            Object.Destroy(mesh);
            Object.Destroy(obj);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            OnExportFinished?.Invoke();
            ObjExporterScript.End();
        }
    }

    private static async Task<ObjExportData> ProcessTransform(
    Transform t,
    bool makeSubmeshes,
    List<Material> materials,
    ObjExportData data)
    {
        data ??= new ObjExportData();

        if (t.TryGetComponent<MeshFilter>(out var mf))
        {
            data.Obj.Append("#" + t.name
                + "\n#-------"
                + "\n");

            if (makeSubmeshes)
            {
                data.Obj.Append("g ").Append(t.name).Append("\n");
            }

            var task = ObjExporterScript.MeshToString(mf, t, materials, data);
            await task;
            if (!Application.isPlaying)
                throw new Exception("App quit during task");

            data.Obj.Append(task.Result);
        }

        return data;
    }

    private static void WriteToFile(string s, string filename)
    {
        using StreamWriter sw = new StreamWriter(filename);
        sw.Write(s);
    }
}

public class ObjExporterScript
{
    private static int _startIndex = 0;

    public static void Start()
    {
        _startIndex = 0;
    }

    public static void End()
    {
        _startIndex = 0;
    }

    private static void HandleTexture(
    string materialPropertyName, 
    Material mat,
    string options,
    ObjExportData data,   
    params string[] mapNames)
    {
        if (!mat.HasProperty(materialPropertyName)) 
            return;

        Texture2D tex = (Texture2D)mat.GetTexture(materialPropertyName);
        if (tex != null)
        {
            options ??= string.Empty;
            string texString = Convert.ToBase64String(tex.EncodeToPNG());
            data.Textures.Add(new ObjExportTexture(tex.name, texString));
            foreach (var item in mapNames)
            {
                Vector2 o = mat.GetTextureOffset(materialPropertyName);
                Vector2 s = mat.GetTextureScale(materialPropertyName);
                Add(data.Mtl, $"{item} -o {o.x} {o.y} -s {s.x} {s.y} {options} {tex.name}.png");
            }
        }
    }

    private static void Add(StringBuilder sb, string text)
    {
        sb.Append(text).Append("\n");
    }

    public static async Task<ObjExportData> MeshToString(
    MeshFilter mf, 
    Transform t, 
    List<Material> materials, 
    ObjExportData data)
    {
        Quaternion rotation = t.localRotation;

        int numVertices = 0;
        Mesh m = mf.sharedMesh;
        if (!m)
        {
            string message = "Mesh not found. Fatal error";
            UI_DialogPrompt.Open(message);
            throw new Exception(message);
        }

        foreach (Vector3 vv in m.vertices)
        {
            Vector3 v = t.TransformPoint(vv);
            numVertices++;
            data.Obj.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
        }

        data.Obj.Append("\n");

        foreach (Vector3 nn in m.normals)
        {
            Vector3 v = rotation * nn;
            data.Obj.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
        }

        data.Obj.Append("\n");

        foreach (Vector3 v in m.uv)
        {
            data.Obj.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        
        for (int subMesh = 0; subMesh < m.subMeshCount; subMesh++)
        {
            data.Obj.Append("\n");

            int submeshIndex = subMesh;
            if (materials[submeshIndex] != null)
            {
                var mat = materials[submeshIndex];
                string name = mat.name;

                data.Obj.Append("usemtl ").Append(name).Append("\n");
                data.Mtl.Append($"newmtl {name}").Append("\n");

                var color = mat.color;
                data.Mtl.Append($"Ka {color.r} {color.g} {color.b}").Append("\n");
                data.Mtl.Append($"Kd {color.r} {color.g} {color.b}").Append("\n");
                data.Mtl.Append($"d {color.a}").Append("\n");
                data.Mtl.Append($"Tr {1 - color.a}").Append("\n");

                //Ke/map_Ke
                if (mat.globalIlluminationFlags != 
                MaterialGlobalIlluminationFlags.EmissiveIsBlack)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color e = mat.GetColor("_EmissionColor");
                        Add(data.Mtl, $"Ke {e.r} {e.g} {e.b}");
                    }
                    
                    if (mat.HasProperty("_EmissionMap"))
                    {
                        HandleTexture("_EmissionMap", mat, null, data, "map_Ke");
                    }
                }
                
                if (mat.HasProperty("_Metallic"))
                {
                    Add(data.Mtl, $"Pm {mat.GetFloat("_Metallic")}");
                    
                }
                HandleTexture("_MetallicGlossMap", mat, null, data, "map_Pm");
                HandleTexture("_BaseMap", mat, null, data, "map_Ka", "map_Kd");

                bool hasBumpScale = false;
                float scale = 0;
                if (mat.HasProperty("_BumpScale"))
                {
                    scale = mat.GetFloat("_BumpScale");
                    hasBumpScale = true;
                }
                string options = hasBumpScale ? $"-bm {scale}" : null;
                HandleTexture("_BumpMap", mat, options, data, "map_bump", "bump");
            }
            else
            {
                data.Obj.Append("usemtl ").Append("Empty").Append("\n");
                data.Mtl.Append($"newmtl Empty").Append("\n");
                data.Mtl.Append($"Ka 1 1 1").Append("\n");
                data.Mtl.Append($"Kd 1 1 1").Append("\n");
            }

            int[] triangles = m.GetTriangles(subMesh);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                data.Obj.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + _startIndex, triangles[i + 1] + 1 + _startIndex, triangles[i + 2] + 1 + _startIndex));
            }

            ObjExporter.OnSubMeshProcessed?.Invoke((float)subMesh / (m.subMeshCount + 1));

            await Task.Yield();
            if (!Application.isPlaying)
                throw new Exception("App quit during task");
        }

        _startIndex += numVertices;
        return data;
    }
}

[Serializable]
public class ObjExportData
{
    public StringBuilder Obj { get; set; } = new();

    public StringBuilder Mtl { get; set; } = new();

    public string ObjString { get; set; }
    public string MtlString { get; set; }

    /// <summary>
    /// Serializes string builders so class
    /// can be properly serialized to json
    /// </summary>
    public void Bake()
    {
        ObjString = Obj.ToString();
        MtlString = Mtl.ToString();
    }

    public List<ObjExportTexture> Textures 
    { get; set; } = new();
}

[Serializable]
public class ObjExportTexture
{
    public ObjExportTexture(string name, string textureBase64)
    {
        Name = name;
        TextureBase64 = textureBase64;
    }

    public string Name { get; set; }
    public string TextureBase64 { get; set; }
}