using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleGroup : MonoBehaviour
{
    [field: SerializeField] public string id { get; private set; }

    void Start()
    {
        Invoke("DelayedListeners", 1f);
    }

    void DelayedListeners()
    {
        ScaleGroupManager.OnScaleLevelChanged += ScaleLevel;
        ScaleGroupManager.OnZScaleChanged += ScaleZ;
    }

    void ScaleLevel(string changedID, Selectable.ScaleLevel scaleLevel)
    {
        if (changedID != id || GetComponent<Selectable>().CurrentScaleLevel == scaleLevel) return;

        GetComponent<Selectable>().SetScaleLevel(scaleLevel, true);
    }

    void ScaleZ(string changedID, float z)
    {
        Debug.Log($"new value: {z} ||| current value: {transform.localScale.z} ||| isNan = {float.IsNaN(z)}");
        if (changedID != id || transform.localScale.z == z || float.IsNaN(z)) return;

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
        Debug.Log("Updated object");
    }
}
