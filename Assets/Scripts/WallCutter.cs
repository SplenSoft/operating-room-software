using Parabox.CSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCutter : MonoBehaviour
{
    private Selectable _selectable;
    private GizmoHandler _gizmoHandler;

    [field: SerializeField, 
    Tooltip("Should be a cube that will cut through the wall")]
    public GameObject CutArea { get; private set; }

    private void Awake()
    {
        _selectable = transform.root.GetComponent<Selectable>();
        _gizmoHandler = transform.root.GetComponent<GizmoHandler>();

        _selectable.OnPlaced.AddListener(UpdateCuts);
        _gizmoHandler.GizmoDragEnded.AddListener(UpdateCuts);
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
}
