using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchHeightToWalls : MonoBehaviour
{
    private void Awake()
    {
        RoomSize.RoomSizeChanged += UpdateScale;
    }

    private void OnDestroy()
    {
        RoomSize.RoomSizeChanged -= UpdateScale;
    }

    private void Start()
    {
        UpdateScale();
    }

    private void UpdateScale(object o = null, EventArgs e = null)
    {
        var wall = RoomBoundary.Instances.Where(item => item.RoomBoundaryType == RoomBoundaryType.WallWest).First();
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, wall.transform.localScale.z);
    }
}
