using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class KeepRelativePosition : MonoBehaviour
{
    [field: SerializeField] private Transform VirtualParent { get; set; }
    [field: SerializeField] private bool HideIfSurfaceIsHidden { get; set; }
    private Vector3 _relativePosition;
    private RoomBoundary _roomBoundary;
    private InGameLight _light;
    private Transform _originalLightParent;

    private void Awake()
    {
        RoomSize.RoomSizeChanged += RoomSizeChanged;
        RecalculateRelativePosition();
        if (VirtualParent != null) 
        {
            _roomBoundary = VirtualParent.GetComponent<RoomBoundary>();
            if (_roomBoundary != null && HideIfSurfaceIsHidden)
            {
                _roomBoundary.VisibilityStatusChanged.AddListener(CheckHideStatus);
            }
        }

        _light = GetComponentInChildren<InGameLight>();
        if (_light != null) 
        {
            _originalLightParent = _light.transform.parent;
        }
    }

    private void RecalculateRelativePosition()
    {
        if (VirtualParent != null) 
        {
            _relativePosition = transform.position - VirtualParent.position;
        }
    }

    public void VirtualParentChanged(Transform virtualParent)
    {
        VirtualParent = virtualParent;
        _roomBoundary = VirtualParent.GetComponent<RoomBoundary>();
        if (_roomBoundary != null && HideIfSurfaceIsHidden)
        {
            _roomBoundary.VisibilityStatusChanged.AddListener(CheckHideStatus);
        }
    }

    public void CheckHideStatus()
    {
        if (_roomBoundary != null && HideIfSurfaceIsHidden)
        {
            bool enabled = _roomBoundary.MeshRenderer.enabled;
            if (_light != null) 
            {
                _light.transform.parent = enabled ? _originalLightParent : null;
            }
            gameObject.SetActive(_roomBoundary.MeshRenderer.enabled);
        }
    }

    public void SelectablePositionChanged()
    {
        RecalculateRelativePosition();
    }

    private void GoToRelativePosition()
    {
        
        transform.position = VirtualParent.position + _relativePosition;
        RecalculateRelativePosition();
    }

    private async void RoomSizeChanged(object sender, EventArgs e)
    {
        if (VirtualParent == null) return;
        await Task.Yield();
        GoToRelativePosition();
    }

    private void OnDestroy()
    {
        RoomSize.RoomSizeChanged -= RoomSizeChanged;
        if (_roomBoundary != null && HideIfSurfaceIsHidden)
        {
            _roomBoundary.VisibilityStatusChanged.RemoveListener(CheckHideStatus);
        }
    }
}
