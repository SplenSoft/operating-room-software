using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        if (scaleLevel.TryGetValue("rows", out string s_rowCount))
        {
            int rowCount = Int32.Parse(s_rowCount);

            Debug.Log($"Boom Head has {attachRow.Length} and Boom Scale only allows {rowCount}");

            for(int i = 1; i <= attachRow.Length; i++)
            {
                Debug.Log($"Row: {i}");
                if(i <= rowCount)
                {
                    Debug.Log("Setting True");
                    ToggleRow(i, true);
                }
                else
                {
                    Debug.Log("Setting False");
                    ToggleRow(i, false);
                }
            }
        }
    }

    void ToggleRow(int i, bool status)
    {
        foreach(GameObject go in attachRow[i-1].entries)
        {
            go.SetActive(status);
        }
    }
}

[Serializable]
public struct Row
{
    [field: SerializeField] public GameObject[] entries;
}
