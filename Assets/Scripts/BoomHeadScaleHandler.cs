using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class BoomHeadScaleHandler : MonoBehaviour
{
    [field: SerializeField] public Row[] attachRow;
    [field: SerializeField] public ColumnAtScale[] attachOffsets;
    private Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    void Start()
    {
        selectable.OnScaleChange.AddListener((x) => ReassembleRows(x));
    }

    int k = 0;
    void ReassembleRows(Selectable.ScaleLevel scaleLevel)
    {
        if (scaleLevel.TryGetValue("rows", out string s_rowCount))
        {
            int rowCount = Int32.Parse(s_rowCount);

            for (int i = 1; i <= attachRow.Length; i++)
            {
                if (i <= rowCount)
                {
                    foreach (GameObject go in attachRow[i - 1].entries)
                    {
                        go.SetActive(true);
                        SetHeight(go, i - 1, rowCount);
                    }
                }
                else
                {
                    foreach (GameObject go in attachRow[i - 1].entries)
                    {
                        go.SetActive(false);
                    }
                }
            }
        }
    }

    void SetHeight(GameObject go, int i, int rowCount)
    {
        go.transform.localPosition = new Vector3(
                go.transform.localPosition.x,
                go.transform.localPosition.y,
                attachOffsets[rowCount - 1].zPosition[i]
            );
    }
}

[Serializable]
public struct Row
{
    [field: SerializeField] public GameObject[] entries;
}

[Serializable]
public struct ColumnAtScale
{
    [field: SerializeField] public float[] zPosition;
}