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

    public static async Task<string> MeshToString(MeshFilter mf, Transform t, List<Material> materials, StringBuilder mtlStringBuilder, Loading.LoadingToken loadingToken)
    {
        Vector3 s = t.localScale;
        Vector3 p = t.localPosition;
        Quaternion rotation = t.localRotation;

        int numVertices = 0;
        Mesh m = mf.sharedMesh;
        if (!m)
        {
            Debug.Log("Export error");
            return "####Error####";
        }

        StringBuilder sb = new StringBuilder();

        foreach (Vector3 vv in m.vertices)
        {
            Vector3 v = t.TransformPoint(vv);
            numVertices++;
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
        }
        sb.Append("\n");
        foreach (Vector3 nn in m.normals)
        {
            Vector3 v = rotation * nn;
            sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        
        //Material[] mats = m.sub
        Debug.Log($"GameObject {t.gameObject.name} has {m.subMeshCount} submeshes");
        for (int subMesh = 0; subMesh < m.subMeshCount; subMesh++)
        {
            sb.Append("\n");

            //Debug.Log($"Submesh {t.name} meshRenderer == null -> {meshRenderer == null}");

            if (materials[subMesh + 1] != null)
            {
                string name = materials[subMesh + 1].name;
                sb.Append("usemtl ").Append(materials[subMesh + 1].name).Append("\n");
                mtlStringBuilder.Append($"newmtl {name}").Append("\n");
                //sb.Append("usemap ").Append(materials[subMesh].name).Append("\n");

                var color = materials[subMesh + 1].color;
                mtlStringBuilder.Append($"Ka {color.r} {color.g} {color.b}").Append("\n");
                mtlStringBuilder.Append($"Kd {color.r} {color.g} {color.b}").Append("\n");
            }
            else
            {
                sb.Append("usemtl ").Append("Empty").Append("\n");
                mtlStringBuilder.Append($"newmtl Empty").Append("\n");
                //sb.Append("usemap ").Append(materials[subMesh].name).Append("\n");

                //var color = materials[subMesh].color;
                mtlStringBuilder.Append($"Ka 1 1 1").Append("\n");
                mtlStringBuilder.Append($"Kd 1 1 1").Append("\n");
            }

            int[] triangles = m.GetTriangles(subMesh);
            //Debug.Log($"Material {mats[material].name} on obj {t.gameObject.name} has {triangles.Length} triangles");
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex));
            }

            loadingToken.SetProgress((float)subMesh / (m.subMeshCount + 1));
            await Task.Yield();
        }

        StartIndex += numVertices;
        return sb.ToString();
    }
}

public static class ObjExporter
{
    //[MenuItem("File/Export/Wavefront OBJ")]
    //static void DoExportWSubmeshes()
    //{
    //    DoExport(true);
    //}

    //[MenuItem("File/Export/Wavefront OBJ (No Submeshes)")]
    //static void DoExportWOSubmeshes()
    //{
    //    DoExport(false);
    //}

    

    public static async void DoExport(bool makeSubmeshes, GameObject obj)
    {
        var loadingTokenOverall = Loading.GetLoadingToken();
        var loadingTokenCombiningMeshes = Loading.GetLoadingToken();
        var loadingTokenUpload = Loading.GetLoadingToken();
        var loadingTokenWaitForResponse = Loading.GetLoadingToken();
        var loadingTokenWriteObj = Loading.GetLoadingToken();
        ObjExporterScript.Start();

        try
        {
            string meshName = obj.name;
            //string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                + "\n#" + System.DateTime.Now.ToLongDateString()
                + "\n#" + System.DateTime.Now.ToLongTimeString()
                + "\n#-------"
                + "\n\n");

            Vector3 oldPos = obj.transform.position;
            obj.transform.position = Vector3.zero;
            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshRenderer>().Where(item => item.enabled).ToList().ConvertAll(item => item.gameObject.GetComponent<MeshFilter>()).ToArray();
            UnityEngine.Debug.Log($"Found {meshFilters.Length} meshfilters");
            //CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            List<Material> materials = new();
            int i = 0;
            while (i < meshFilters.Length)
            {
                var filter = meshFilters[i];
                var meshRenderer = filter.gameObject.GetComponent<MeshRenderer>();
                for (int j = 0; j < filter.sharedMesh.subMeshCount; j++)
                {
                    CombineInstance instance = new();
                    instance.mesh = filter.sharedMesh;
                    instance.transform = filter.transform.localToWorldMatrix;
                    instance.subMeshIndex = j;

                    if (meshRenderer.sharedMaterials.Length > j)
                    {
                        materials.Add(meshRenderer.sharedMaterials[j]);
                    }
                    else
                    {
                        materials.Add(null);
                    }
                    combineInstances.Add(instance);
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
            obj.transform.position = oldPos;

            obj = new GameObject("Mesh", typeof(MeshFilter)/*, typeof(MeshRenderer)*/);
            //obj.GetComponent<MeshRenderer>()
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;

            Transform t = obj.transform;

            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            if (!makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }
            StringBuilder mtlStringBuilder = new StringBuilder();
            var task = ProcessTransform(t, makeSubmeshes, materials, mtlStringBuilder, loadingTokenWriteObj);
            await task;
            meshString.Append(task.Result);
            loadingTokenOverall.SetProgress(0.75f);
            string id = Guid.NewGuid().ToString();

            meshString.Append($"mtllib {id}.mtl");

            Dictionary<string, string> formFields = new()
            {
                { "objData", meshString.ToString() },
                { "mtlData", mtlStringBuilder.ToString() },
                { "app_password", "qweasdv413240897fvhw" },
                { "id", id }
            };

            using UnityWebRequest request = UnityWebRequest.Post("http://www.splensoft.com/ors/php/export-obj.php", formFields);
            request.SendWebRequest();

            while (!request.isDone)
            {
                loadingTokenUpload.SetProgress(request.uploadProgress / 1.01f);
                float waitingProgress = Mathf.Min(loadingTokenWaitForResponse.Progress + (0.01f * Time.deltaTime), 0.9f);
                loadingTokenWaitForResponse.SetProgress(waitingProgress);
                await Task.Yield();
                if (!Application.isPlaying) return;
            }

            loadingTokenUpload.SetProgress(1f / 1.01f);

            Debug.Log(request.downloadHandler.text);

            if (request.responseCode != 200)
            {
                Debug.LogError(request.responseCode);
            }

            if (request.error != null)
            {
                Debug.LogError(request.error);
                return;
            }

            if (request.downloadHandler.text.StartsWith("bad password"))
            {
                Debug.LogError("Bad app password");
                return;
            }

            if (request.downloadHandler.text == "success")
            {
                Application.OpenURL("http://www.splensoft.com/ors/obj.html?id=" + id);
            }
            else
            {
                Debug.LogError("Something went wrong while getting PDF URL");
                Debug.LogError(request);
            }

            
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

    private static async Task<string> ProcessTransform(Transform t, bool makeSubmeshes, List<Material> materials, StringBuilder mtlStringBuilder, Loading.LoadingToken loadingToken)
    {
        StringBuilder meshString = new StringBuilder();

        MeshFilter mf = t.GetComponent<MeshFilter>();
        //MeshRenderer mr = t.GetComponent<MeshRenderer>();

        if (mf != null /*&& mr != null && mr.enabled*/)
        {
            meshString.Append("#" + t.name
                          + "\n#-------"
                          + "\n");
            if (makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }
            var task = ObjExporterScript.MeshToString(mf, t, materials, mtlStringBuilder, loadingToken);
            await task;
            meshString.Append(task.Result);
        }

        for (int i = 0; i < t.childCount; i++)
        {
            var task = ProcessTransform(t.GetChild(i), makeSubmeshes, materials, mtlStringBuilder, loadingToken);
            await task;
            meshString.Append(task.Result);
        }

        return meshString.ToString();
    }

    static void WriteToFile(string s, string filename)
    {
        using StreamWriter sw = new StreamWriter(filename);
        sw.Write(s);
    }
}