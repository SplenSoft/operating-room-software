﻿using System;
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

    /// <summary>
    /// Will listen to events from these items to reset the scale
    /// </summary>
    [field: SerializeField]
    private List<GizmoHandler> GizmoHandlers { get; set; } = new();

    /// <summary>
    /// Will listen to events from these items to reset the scale
    /// </summary>
    [field: SerializeField]
    private List<RoomBoundary> RoomBoundaries { get; set; } = new();

    /// <summary>
    /// Will listen to events from these items to reset the scale
    /// </summary>
    [field: SerializeField]
    private List<MatchHeightToWalls> MatchHeightToWalls { get; set; } = new();

    [field: SerializeField]
    private List<Cuttable> Cuttables { get; set; } = new();

    [field: SerializeField]
    private MeshInstanceManager MeshInstanceManager { get; set; }

    private UnityEventManager _eventManager = new();

    private void Awake()
    {
        //_meshRenderer = GetComponentInChildren<MeshRenderer>();
        _meshFilter = GetComponentInChildren<MeshFilter>();
        MeshInstanceManager.RegisterMesh(_meshFilter.sharedMesh);

        GizmoHandlers.ForEach(x => _eventManager.RegisterEvent(x.GizmoDragEnded, UpdateMaterials));
        MatchHeightToWalls.ForEach(x => _eventManager.RegisterEvent(x.HeightSet, UpdateMaterials));
        RoomBoundaries.ForEach(x => _eventManager.RegisterEvent(x.SizeSet, UpdateMaterials));
        Cuttables.ForEach(x => _eventManager.RegisterEvent(x.OnCutComplete, UpdateMaterials));

        _eventManager.AddListeners();
    }

    private void OnDestroy()
    {
        _eventManager.RemoveListeners();
    }

    private void Start()
	{
        UpdateMaterials();
    }

	private void UpdateMaterials()
	{
        //Debug.Log($"Updating UVs for game object {gameObject.name}");

        if (_meshFilter)
        {
            var mesh = MeshInstanceManager.InstancedMesh;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

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

            mesh.uv = uvs;

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
