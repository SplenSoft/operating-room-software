using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BoomHeadScaleHandler : MonoBehaviour
{
    [field: SerializeField] public Row[] attachRow;
    private Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    void Start()
    {
        selectable.OnScaleChange.AddListener((x) => ReassembleRows(x));
    }

    void ReassembleRows(Selectable.ScaleLevel scaleLevel)
    {
        if (scaleLevel.TryGetValue("rows", out string rowCount))
            Debug.Log(Int32.Parse(rowCount));
    }
}

[Serializable]
public struct Row
{
    [field: SerializeField] public GameObject[] entries;
}
