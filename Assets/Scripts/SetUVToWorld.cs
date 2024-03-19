using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SetUVToWorld : MonoBehaviour
{
	// set this if you are putting it on a parent object, otherwise
	// this script operates only on the current GameObject.
	public bool DriveToAllChildren;

	//public Material MaterialToUse;

	public bool PreserveColor;

	public bool IncludeRotation;
	//private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private GizmoHandler _gizmoHandler;
    private RoomBoundary _roomBoundary;

    //private void Reset()
    //{
    //	IncludeRotation = true;
    //	MaterialToUse = null;
    //}

    //public static void AddToAllMeshRenderersWithMeshFilters(
    //	GameObject go,
    //	bool _PreserveColor = false,
    //	Material _MaterialToUse = null)
    //{
    //	MeshRenderer[] rndrrs = go.GetComponentsInChildren<MeshRenderer> ();
    //	foreach( var mr in rndrrs)
    //	{
    //		MeshFilter mf = mr.GetComponent<MeshFilter> ();
    //		if (mf != null)
    //		{
    //			var uvsetter = mr.gameObject.AddComponent<SetUVToWorld> ();
    //			uvsetter.PreserveColor = _PreserveColor;
    //			uvsetter.MaterialToUse = _MaterialToUse;
    //		}
    //	}
    //}

    private void Awake()
    {
        //_meshRenderer = GetComponentInChildren<MeshRenderer>();
        _meshFilter = GetComponentInChildren<MeshFilter>();
        _gizmoHandler = GetComponentInChildren<GizmoHandler>();
        _roomBoundary = GetComponentInChildren<RoomBoundary>();

        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.AddListener(UpdateMaterials);
        }

        if (_roomBoundary != null)
        {
            _roomBoundary.SizeSet.AddListener(UpdateMaterials);
        }
    }

    private void UpdateMaterials(RoomDimension arg0) => UpdateMaterials();

    private void OnDestroy()
    {
        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.RemoveListener(UpdateMaterials);
        }

        if (_roomBoundary != null)
        {
            _roomBoundary.SizeSet.RemoveListener(UpdateMaterials);
        }
    }

    private void Start()
	{
        UpdateMaterials();
    }

	private void UpdateMaterials()
	{
        if (_meshFilter)
        {
            Vector2[] uvs = _meshFilter.mesh.uv;
            Vector3[] verts = _meshFilter.mesh.vertices;
            int[] tris = _meshFilter.mesh.triangles;

            if (uvs.Length != verts.Length)
            {
                uvs = new Vector2[verts.Length];
            }

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = _meshFilter.transform.TransformPoint(verts[i]);
                if (!IncludeRotation)
                {
                    verts[i] = Quaternion.Inverse(_meshFilter.transform.rotation) * verts[i];
                }
            }

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 norm = Vector3.Cross(
                    verts[tris[i + 1]] - verts[tris[i + 0]],
                    verts[tris[i + 1]] - verts[tris[i + 2]]).normalized;

                float dotX = Mathf.Abs(Vector3.Dot(norm, Vector3.right));
                float dotY = Mathf.Abs(Vector3.Dot(norm, Vector3.up));
                float dotZ = Mathf.Abs(Vector3.Dot(norm, Vector3.forward));

                if (dotX > dotY && dotX > dotZ)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        uvs[tris[i + j]] = new Vector2(verts[tris[i + j]].z, verts[tris[i + j]].y);
                    }
                }
                else
                {
                    if (dotY > dotX && dotY > dotZ)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            uvs[tris[i + j]] = new Vector2(verts[tris[i + j]].x, verts[tris[i + j]].z);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            uvs[tris[i + j]] = new Vector2(verts[tris[i + j]].x, verts[tris[i + j]].y);
                        }
                    }
                }
            }

            _meshFilter.mesh.uv = uvs;

            //var MaterialToUse = _meshRenderer.materials[0];
            //if (MaterialToUse)
            //{
            //    Dictionary<Color, Material> ColorDictToSaveDrawcalls = new Dictionary<Color, Material>();

            //    Renderer rndrr = _meshRenderer;
            //    if (rndrr)
            //    {
            //        Material[] allNewMaterials = new Material[rndrr.materials.Length];

            //        for (int i = 0; i < allNewMaterials.Length; i++)
            //        {
            //            Color preservedColor = Color.white;

            //            Material originalMaterial = rndrr.materials[i];

            //            var instancedMaterial = MaterialToUse;

            //            if (originalMaterial)
            //            {
            //                if (PreserveColor)
            //                {
            //                    preservedColor = originalMaterial.color;

            //                    if (ColorDictToSaveDrawcalls.ContainsKey(preservedColor))
            //                    {
            //                        instancedMaterial = ColorDictToSaveDrawcalls[preservedColor];
            //                    }
            //                    else
            //                    {
            //                        instancedMaterial = new Material(MaterialToUse);
            //                        instancedMaterial.color = preservedColor;
            //                        ColorDictToSaveDrawcalls[preservedColor] = instancedMaterial;
            //                    }
            //                }
            //            }

            //            allNewMaterials[i] = instancedMaterial;
            //        }

            //        rndrr.materials = allNewMaterials;
            //    }
            //}
        }
        else
        {
            Debug.LogError(
                GetType() + ": there is no MeshFilter on GameObject '" + name + "'!");
        }
    }
}
