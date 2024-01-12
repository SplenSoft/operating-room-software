using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GizmoSetting
{
    [field: SerializeField] public Axis Axis { get; private set; }
    [field: SerializeField] public GizmoType GizmoType { get; private set; }
    [field: SerializeField] public bool Unrestricted { get; private set; } = true;
    [field: SerializeField] private float MaxValue { get; set; }
    [field: SerializeField] private float MinValue { get; set; }

    /// <summary>
    /// When calculating max and min heights for elevation photos, treats min as max and vice versa (fixes some issues with y-axis rotations)
    /// </summary>
    [field: SerializeField] public bool Invert { get; private set; }

    public float GetMaxValue => Unrestricted ? float.MaxValue : MaxValue;
    public float GetMinValue => Unrestricted ? float.MinValue : MinValue;
}

public enum Axis
{
    X,
    Y,
    Z
}

public enum GizmoType
{
    Move,
    Rotate,
    Scale
}