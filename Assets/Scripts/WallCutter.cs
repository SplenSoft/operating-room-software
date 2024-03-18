using Parabox.CSG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class WallCutter : MonoBehaviour
{
    private Selectable _selectable;
    private GizmoHandler _gizmoHandler;
    private MeshRenderer _meshRenderer;
    public Collider Collider { get; private set; }

    [field: SerializeField, 
    Tooltip("Should be a cube that will cut through the wall")]
    public GameObject CutArea { get; private set; }

    private void Awake()
    {
        Collider = CutArea.GetComponent<Collider>();
        _selectable = transform.root.GetComponent<Selectable>();
        _gizmoHandler = transform.root.GetComponent<GizmoHandler>();
        _meshRenderer = CutArea.GetComponent<MeshRenderer>();

        _selectable.OnPlaced.AddListener(UpdateCuts);
        _gizmoHandler.GizmoDragEnded.AddListener(UpdateCuts);
    }

    private void Start()
    {
        _meshRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        _selectable.OnPlaced.RemoveListener(UpdateCuts);
        _gizmoHandler.GizmoDragEnded.RemoveListener(UpdateCuts);
    }

    private void UpdateCuts()
    {
        Cuttable.UpdateCuts();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WallCutter))]
    private class WallCutter_Inspector : Editor
    {
        private WallCutter _target;

        public override void OnInspectorGUI()
        {
            if (_target == null)
            {
                _target = target as WallCutter;
            }

            if (_target.CutArea != null)
            {
                if (GUILayout.Button("Encapsulate Meshes"))
                {
                    var childRenderer = _target.CutArea.GetComponent<MeshRenderer>();
                    var meshes = _target.transform.root
                        .GetComponentsInChildren<MeshRenderer>()
                        .Where(x => x != childRenderer && 
                        x.transform.parent.GetComponent<WallCutter>() == null)
                        .ToList();

                    var bounds = meshes[0].bounds;

                    for (int i = 1; i < meshes.Count; i++)
                    {
                        bounds.Encapsulate(meshes[i].bounds);
                    }

                    Vector3 pos = _target.CutArea.transform.position;
                    pos.x = bounds.center.x;
                    pos.y = bounds.center.y;
                    _target.CutArea.transform.position = pos;

                    Vector3 scale = _target.CutArea.transform.localScale;
                    scale.x = bounds.size.x;
                    scale.y = bounds.size.y;
                    _target.CutArea.transform.localScale = scale;

                    EditorUtility.SetDirty(target);
                }
            }

            DrawDefaultInspector();
        }
    }
#endif
}