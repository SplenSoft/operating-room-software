using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Selectable))]
public class GizmoHandler : MonoBehaviour
{
    private ObjectTransformGizmo _translateGizmo;
    private ObjectTransformGizmo _rotateGizmo;
    private ObjectTransformGizmo _scaleGizmo;
    private ObjectTransformGizmo _universalGizmo;
    private bool _gizmosInitialized;
    public static bool GizmoBeingUsed { get; private set; }
    public bool GizmoUsedLastFrame { get; private set; }
    
    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        GizmoSelector.GizmoModeChanged += GizmoModeChanged;
    }

    private void GizmoModeChanged(object sender, EventArgs e)
    {
        EnableGizmo();
    }

    private void OnDestroy()
    {
        GizmoSelector.GizmoModeChanged -= GizmoModeChanged;
    }

    public void SelectableSelected()
    {
        CreateGizmos();
        EnableGizmo();
    }

    public void SelectableDeselected()
    {
        EnableGizmo();
    }

    private void EnableGizmo()
    {
        if (!_gizmosInitialized) return;
        _translateGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Translate && _selectable.IsSelected);
        _translateGizmo.Gizmo.Transform.Position3D = transform.position;

        _rotateGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Rotate && _selectable.IsSelected);
        _rotateGizmo.Gizmo.Transform.Position3D = transform.position;

        _scaleGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Scale && _selectable.IsSelected);
        _scaleGizmo.Gizmo.Transform.Position3D = transform.position;

        _universalGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Universal && _selectable.IsSelected);
        _universalGizmo.Gizmo.Transform.Position3D = transform.position;
    }

    private void CreateGizmos()
    {
        if (_gizmosInitialized) return;

        _translateGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
        _translateGizmo.SetTargetObject(gameObject);
        _translateGizmo.Gizmo.MoveGizmo.SetVertexSnapTargetObjects(new List<GameObject> { gameObject });
        _translateGizmo.SetTransformSpace(GizmoSpace.Local);
        _translateGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _translateGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _translateGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;

        _rotateGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
        _rotateGizmo.SetTargetObject(gameObject);
        _rotateGizmo.SetTransformSpace(GizmoSpace.Local);
        _rotateGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _rotateGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _rotateGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;

        _scaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        _scaleGizmo.SetTargetObject(gameObject);
        _scaleGizmo.SetTransformSpace(GizmoSpace.Local);
        _scaleGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _scaleGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _scaleGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;

        _universalGizmo = RTGizmosEngine.Get.CreateObjectUniversalGizmo();
        _universalGizmo.SetTargetObject(gameObject);
        _universalGizmo.Gizmo.UniversalGizmo.SetMvVertexSnapTargetObjects(new List<GameObject> { gameObject });
        _universalGizmo.SetTransformSpace(GizmoSpace.Local);
        _universalGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _universalGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _universalGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;

        _gizmosInitialized = true;
    }

    private async void OnGizmoPostDragEnd(Gizmo gizmo, int handleId)
    {
        SendMessage("SelectablePositionChanged");
        GizmoBeingUsed = false;
        await Task.Yield();
        GizmoUsedLastFrame = false;
    }

    private void OnGizmoPostDragBegin(Gizmo gizmo, int handleId)
    {
        GizmoBeingUsed = true;
    }

    private void OnGizmoPostDragUpdate(Gizmo gizmo, int handleId)
    {
        GizmoUsedLastFrame = true;
    }
}

public enum GizmoMode
{
    Translate,
    Rotate,
    Scale,
    Universal
}
