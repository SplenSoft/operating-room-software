using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;
using iDiscGolf;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;

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

    public static string MeshToString(MeshFilter mf, Transform t)
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

        //Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
        //Material[] mats = m.sub
        Debug.Log($"GameObject {t.gameObject.name} has {m.subMeshCount} submeshes");
        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            //sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            //sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
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

        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshRenderer>().Where(item => item.enabled).ToList().ConvertAll(item => item.gameObject.GetComponent<MeshFilter>()).ToArray();
        Debug.Log($"Found {meshFilters.Length} meshfilters");
        //CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        List<CombineInstance> combineInstances = new List<CombineInstance>();

        int i = 0;
        while (i < meshFilters.Length)
        {
            var filter = meshFilters[i];
            for (int j = 0; j < filter.sharedMesh.subMeshCount; j++)
            {
                CombineInstance instance = new CombineInstance();
                instance.mesh = filter.sharedMesh;
                instance.transform = filter.transform.localToWorldMatrix;
                instance.subMeshIndex = j;
                combineInstances.Add(instance);
            }

            i++;
        }

        CombineInstance[] combine = combineInstances.ToArray();

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, mergeSubMeshes: false, useMatrices: true);

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
        meshString.Append(ProcessTransform(t, makeSubmeshes));

#if UNITY_EDITOR
        string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");
        //WriteToFile(meshString.ToString(), Application.persistentDataPath + "/exportedObj.obj");
        WriteToFile(meshString.ToString(), fileName);
#elif UNITY_WEBGL
        WebGLExtern.SaveStringToFile(meshString.ToString());
#else
        throw new Exception("Not supported on this platform");
#endif

        t.position = originalPosition;

        ObjExporterScript.End();
        Debug.Log("Exported Mesh: " + fileName);
        Object.Destroy(mesh);
        Object.Destroy(obj);
    }

    static string ProcessTransform(Transform t, bool makeSubmeshes)
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
            meshString.Append(ObjExporterScript.MeshToString(mf, t));
        }

        for (int i = 0; i < t.childCount; i++)
        {
            meshString.Append(ProcessTransform(t.GetChild(i), makeSubmeshes));
        }

        return meshString.ToString();
    }

    static void WriteToFile(string s, string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            sw.Write(s);
        }
    }
}