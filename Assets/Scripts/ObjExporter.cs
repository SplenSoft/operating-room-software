using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using static UnityEditor.PlayerSettings;

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

    public static string MeshToString(MeshFilter mf, Transform t, List<Material> materials, StringBuilder mtlStringBuilder)
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

    

    public static void DoExport(bool makeSubmeshes, GameObject obj)
    {
        string meshName = obj.name;
        //string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");

        ObjExporterScript.Start();

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
        }
        Debug.Log($"Combining {combineInstances.Count} combine instances ...");
        CombineInstance[] combine = combineInstances.ToArray();

        Mesh mesh = new();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.CombineMeshes(combine, mergeSubMeshes: false, useMatrices: true);
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
        meshString.Append(ProcessTransform(t, makeSubmeshes, materials, mtlStringBuilder));

        

#if UNITY_EDITOR
        string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");
        //WriteToFile(meshString.ToString(), Application.persistentDataPath + "/exportedObj.obj");
        string mtlFilePath = fileName.Replace(".obj", ".mtl");
        int pos = mtlFilePath.LastIndexOf("/") + 1;
        string mtlFileName = mtlFilePath.Substring(pos, mtlFilePath.Length - pos);
        meshString.Append($"mtllib {mtlFileName}");
        WriteToFile(meshString.ToString(), fileName);
        WriteToFile(mtlStringBuilder.ToString(), mtlFilePath);
        UnityEngine.Debug.Log("Exported Mesh: " + fileName);
#elif UNITY_WEBGL
        WebGLExtern.SaveStringToFile(meshString.ToString(), "obj");
#elif UNITY_STANDALONE_WIN
        string fileName = Application.persistentDataPath + $"/ExportedArmAssemblyElevationOBJ_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.obj";
        WriteToFile(meshString.ToString(), fileName);
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            Verb = "open",
            FileName = Application.persistentDataPath,
        };

        Process.Start(startInfo);
#else
        throw new System.Exception("Not supported on this platform");
#endif

        t.position = originalPosition;

        ObjExporterScript.End();
        
        Object.Destroy(mesh);
        Object.Destroy(obj);
    }

    static string ProcessTransform(Transform t, bool makeSubmeshes, List<Material> materials, StringBuilder mtlStringBuilder)
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
            meshString.Append(ObjExporterScript.MeshToString(mf, t, materials, mtlStringBuilder));
        }

        for (int i = 0; i < t.childCount; i++)
        {
            meshString.Append(ProcessTransform(t.GetChild(i), makeSubmeshes, materials, mtlStringBuilder));
        }

        return meshString.ToString();
    }

    static void WriteToFile(string s, string filename)
    {
        using StreamWriter sw = new StreamWriter(filename);
        sw.Write(s);
    }
}