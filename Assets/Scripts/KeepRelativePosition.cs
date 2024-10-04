using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class KeepRelativePosition : MonoBehaviour
{
    [field: SerializeField] 
    private Transform VirtualParent { get; set; }

    [field: SerializeField] 
    private bool HideIfSurfaceIsHidden { get; set; }

    /// <summary>
    /// Populated when an object is loaded by the 
    /// <see cref="ConfigurationManager"/>
    /// </summary>
    public string ParentName { get; set; }

    private Vector3 _relativePosition;
    private RoomBoundary _roomBoundary;
    private InGameLight _light;
    private Transform _originalLightParent;
    private bool _subscribedRoomSizeChange;
    private bool _subscribedVisibilityChanged;
    private bool _isDestroyed;

    private void Awake()
    {
        RecalculateRelativePosition();
        SubscribeRoomSizeChange();
        SubscribeVisibility();

        _light = GetComponentInChildren<InGameLight>();

        if (_light != null) 
        {
            _originalLightParent = _light.transform.parent;
        }
    }

    private void SubscribeRoomSizeChange()
    {
        if (_subscribedRoomSizeChange) 
            return;

        _subscribedRoomSizeChange = true;
        ConfigurationManager.OnRoomLoadComplete.AddListener(TryGetParent);
        RoomSize.RoomSizeChanged.AddListener(RoomSizeChanged);
    }

    private void SubscribeVisibility()
    {
        if (VirtualParent == null || _subscribedVisibilityChanged) 
            return;

        _roomBoundary = VirtualParent.GetComponent<RoomBoundary>();

        if (_roomBoundary != null && HideIfSurfaceIsHidden)
        {
            _roomBoundary.VisibilityStatusChanged
                .AddListener(CheckHideStatus);

            UI_ToggleShowCeilingObjects
                .CeilingObjectVisibilityToggled
                .AddListener(CheckHideStatus);

            _subscribedVisibilityChanged = true;
        }
    }

    private void OnDestroy()
    {
        _isDestroyed = true;

        if (_subscribedRoomSizeChange) 
        {
            RoomSize.RoomSizeChanged.RemoveListener(RoomSizeChanged);
        }

        if (_subscribedVisibilityChanged)
        {
            _roomBoundary.VisibilityStatusChanged
                .RemoveListener(CheckHideStatus);

            UI_ToggleShowCeilingObjects
                .CeilingObjectVisibilityToggled
                .RemoveListener(CheckHideStatus);
        }
    }

    private void TryGetParent()
    {
        if (string.IsNullOrEmpty(ParentName)) 
            return;

        var rootObj = GameObject.Find(ParentName);

        if (rootObj == null) 
            return;

        VirtualParent = rootObj.transform;
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
        SubscribeVisibility();
    }

    public void CheckHideStatus()
    {
        if (_roomBoundary != null && HideIfSurfaceIsHidden)
        {
            bool enabled = _roomBoundary.MeshRenderer.enabled || (_roomBoundary.RoomBoundaryType == RoomBoundaryType.Ceiling && UI_ToggleShowCeilingObjects.ShowCeilingObjects);
            if (_light != null) 
            {
                _light.transform.parent = enabled ? _originalLightParent : null;
            }
            gameObject.SetActive(enabled);
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

    private async void RoomSizeChanged(RoomDimension dimension)
    {
        if (VirtualParent == null) 
            return;

        await Task.Yield();

        if (!Application.isPlaying) 
            throw new Exception("App quit during async");

        if (_isDestroyed) 
            return;

        GoToRelativePosition();
    }
}
