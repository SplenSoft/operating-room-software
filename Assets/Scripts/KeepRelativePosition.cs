using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class KeepRelativePosition : MonoBehaviour
{
    [field: SerializeField] private Transform VirtualParent { get; set; }
    private Vector3 _relativePosition;

    private void Awake()
    {
        RoomSize.RoomSizeChanged += RoomSizeChanged;
        RecalculateRelativePosition();
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
        await Task.Yield();
        GoToRelativePosition();
    }

    private void OnDestroy()
    {
        RoomSize.RoomSizeChanged -= RoomSizeChanged;
    }
}
