using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using UnityEngine.Networking;
using System.Threading.Tasks;
using RTG;
using SimpleJSON;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using SplenSoft.UnityUtilities;

public class ObjExporterScript
{
    private static int StartIndex = 0;

    public static void Start()
    {
        StartIndex = 0;
    }
    public static void End()
    {
        StartIndex = 0;
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
    ObjExportData data, 
    Loading.LoadingToken loadingToken)
    {
        Vector3 s = t.localScale;
        Vector3 p = t.localPosition;
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
        
        //Material[] mats = m.sub
        Debug.Log($"GameObject {t.gameObject.name} has {m.subMeshCount} submeshes");
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
                //sb.Append("usemap ").Append(materials[subMesh].name).Append("\n");

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
                //sb.Append("usemap ").Append(materials[subMesh].name).Append("\n");

                //var color = materials[subMesh].color;
                data.Mtl.Append($"Ka 1 1 1").Append("\n");
                data.Mtl.Append($"Kd 1 1 1").Append("\n");
            }

            int[] triangles = m.GetTriangles(subMesh);
            //Debug.Log($"Material {mats[material].name} on obj {t.gameObject.name} has {triangles.Length} triangles");
            for (int i = 0; i < triangles.Length; i += 3)
            {
                data.Obj.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex));
            }

            loadingToken.SetProgress((float)subMesh / (m.subMeshCount + 1));
            await Task.Yield();

            if (!Application.isPlaying)
                throw new AppQuitInTaskException();
        }

        StartIndex += numVertices;
        return data;
    }
}

public static class ObjExporter
{  
    public static async void DoExport(bool makeSubmeshes, MeshFilter[] meshFilters, string name)
    {
        var loadingTokenOverall = Loading.GetLoadingToken();
        var loadingTokenCombiningMeshes = Loading.GetLoadingToken();
        var loadingTokenUpload = Loading.GetLoadingToken();
        var loadingTokenWaitForResponse = Loading.GetLoadingToken();
        var loadingTokenWriteObj = Loading.GetLoadingToken();
        ObjExporterScript.Start();

        try
        {
            string meshName = name;
            

            //Vector3 oldPos = obj.transform.position;
            //obj.transform.position = Vector3.zero;

            UnityEngine.Debug.Log($"Found {meshFilters.Length} meshfilters");
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
                loadingTokenCombiningMeshes.SetProgress((float)i / (meshFilters.Length + 1));
                await Task.Yield();
            }
            Debug.Log($"Combining {combineInstances.Count} combine instances ...");
            CombineInstance[] combine = combineInstances.ToArray();

            Mesh mesh = new();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine, mergeSubMeshes: false, useMatrices: true);
            loadingTokenOverall.SetProgress(0.25f);
            await Task.Yield();
            //obj.transform.position = oldPos;

            ObjExportData data = new ObjExportData();

            data.Obj.Append("#" + meshName + ".obj"
                + "\n#" + System.DateTime.Now.ToLongDateString()
                + "\n#" + System.DateTime.Now.ToLongTimeString()
                + "\n#-------"
                + "\n\n");

            var obj = new GameObject("Mesh", typeof(MeshFilter)/*, typeof(MeshRenderer)*/);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;

            Transform t = obj.transform;

            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            if (!makeSubmeshes)
            {
                data.Obj.Append("g ").Append(t.name).Append("\n");
            }

            var task = ProcessTransform(t, makeSubmeshes, materials, data, loadingTokenWriteObj);
            await task;
            loadingTokenOverall.SetProgress(0.75f);
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

            Application.OpenURL(path);

            //Dictionary<string, string> formFields = new()
            //{
            //    { "json", JsonConvert.SerializeObject(data) },
            //    { "app_password", "qweasdv413240897fvhw" },
            //    { "id", id }
            //};

            //using UnityWebRequest request = UnityWebRequest.Post("http://www.splensoft.com/ors/php/export-obj.php", formFields);
            //request.SendWebRequest();

            //while (!request.isDone)
            //{
            //    loadingTokenUpload.SetProgress(request.uploadProgress / 1.01f);
            //    float waitingProgress = Mathf.Min(loadingTokenWaitForResponse.Progress + (0.01f * Time.deltaTime), 0.9f);
            //    loadingTokenWaitForResponse.SetProgress(waitingProgress);
            //    await Task.Yield();
            //    if (!Application.isPlaying) return;
            //}

            //loadingTokenUpload.SetProgress(1f / 1.01f);

            //Debug.Log(request.downloadHandler.text);

            //if (request.responseCode != 200)
            //{
            //    Debug.LogError(request.responseCode);
            //}

            //if (request.error != null)
            //{
            //    Debug.LogError(request.error);
            //    return;
            //}

            //if (request.downloadHandler.text.StartsWith("bad password"))
            //{
            //    Debug.LogError("Bad app password");
            //    return;
            //}

            //if (request.downloadHandler.text == "success")
            //{
            //    Application.OpenURL("http://www.splensoft.com/ors/obj.html?id=" + id);
            //}
            //else
            //{
            //    Debug.LogError("Something went wrong while getting PDF URL");
            //    Debug.LogError(request);
            //}

            Object.Destroy(mesh);
            Object.Destroy(obj);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            loadingTokenOverall.Done();
            loadingTokenCombiningMeshes.Done();
            loadingTokenUpload.Done();
            loadingTokenWaitForResponse.Done();
            loadingTokenWriteObj.Done();
            ObjExporterScript.End();
        }
    }

    public static void DoExport(bool makeSubmeshes, GameObject obj, ObjExportOptions options)
    {
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshRenderer>()
            .Where(x => FilterMeshRenderers(x, options))
            .ToList()
            .ConvertAll(item => item.gameObject.GetComponent<MeshFilter>())
            .ToArray();

        DoExport(makeSubmeshes, meshFilters, obj.name);
    }

    public static void DoExport(
    bool makeSubmeshes, 
    List<Selectable> selectables,
    ObjExportOptions options)
    {
        MeshFilter[] meshFilters = selectables
            .Where(x => x.transform.root == x.transform)
            .Where(x => 
            {
                if (x.TryGetComponent<RoomBoundary>(out var roomBoundary))
                {
                    switch (roomBoundary.RoomBoundaryType)
                    {
                        case RoomBoundaryType.Ceiling:
                            return options.IncludeCeiling;
                        case RoomBoundaryType.Floor:
                            return options.IncludeFloor;
                        case RoomBoundaryType.WallSouth:
                        case RoomBoundaryType.WallNorth:
                        case RoomBoundaryType.WallEast:
                        case RoomBoundaryType.WallWest:
                            return options.IncludeWalls;
                    }
                }

                if (x.TryGetComponent<Selectable>(out var selectable))
                {
                    if (selectable.SpecialTypes.Contains(SpecialSelectableType.Mount)) 
                    {
                        return options.IncludeArmBoomAssemblies;
                    }

                    float angle = Vector3.Angle(selectable.transform.forward, Vector3.down);

                    if (angle < 5)
                    {
                        return options.IncludeCeilingObjects;
                    }

                    if (angle > 175)
                    {
                        return options.IncludeFloorObjects;
                    }

                    return options.IncludeWallObjects;
                }

                Debug.LogWarning($"Could not determine OBJ export category for {x.gameObject.name}");

                return false;
            })
            .SelectMany(x => x.GetComponentsInChildren<MeshRenderer>())
            .Where(x => FilterMeshRenderers(x, options))
            .ToList()
            .ConvertAll(x => x.gameObject.GetComponent<MeshFilter>())
            .ToArray();

        DoExport(makeSubmeshes, meshFilters, "Room export");
    }

    private static bool FilterMeshRenderers(MeshRenderer meshRenderer, ObjExportOptions options)
    {
        if (!meshRenderer.enabled)
            return false;

        if (options.IncludeArmBoomHeads)
        {
            return true;
        }

        Transform parent = meshRenderer.transform;

        while (parent != null)
        {
            // Best way to determine if something is
            // a boom/arm head
            if (parent.GetComponent<CCDIK>() != null)
            {
                return false;
            }

            parent = parent.parent;
        }

        return true;
    }

    private static async Task<ObjExportData> ProcessTransform(
    Transform t, 
    bool makeSubmeshes, 
    List<Material> materials,
    ObjExportData data, 
    Loading.LoadingToken loadingToken)
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
            var task = ObjExporterScript.MeshToString(mf, t, materials, data, loadingToken);
            await task;
            data.Obj.Append(task.Result);
        }

        for (int i = 0; i < t.childCount; i++)
        {
            var task = ProcessTransform(t.GetChild(i), makeSubmeshes, materials, data, loadingToken);
            await task;
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

[Serializable]
public class ObjExportData
{
    [JsonIgnore]
    public StringBuilder Obj { get; set; } = new();

    [JsonIgnore]
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

public class ObjExportOptions
{
    public bool IncludeFloor { get; set; }
    public bool IncludeFloorObjects { get; set; }
    public bool IncludeWalls { get; set; }
    public bool IncludeWallObjects { get; set; }
    public bool IncludeCeiling { get; set; }
    public bool IncludeCeilingObjects { get; set; }
    public bool IncludeArmBoomAssemblies { get; set; }
    public bool IncludeArmBoomHeads { get; set; }
}