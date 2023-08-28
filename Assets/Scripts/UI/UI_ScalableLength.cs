using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_ScalableLength : MonoBehaviour
{
    [field: SerializeField] private TextMeshProUGUI TextLength { get; set; }

    private void Awake()
    {
        Selectable.SelectionChanged += SeletableChanged;
        gameObject.SetActive(false);
    }

    private void SeletableChanged(object sender, EventArgs e)
    {
        gameObject.SetActive(Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.ScaleLevels.Count > 0);
    }

    private void Update()
    {
        if (Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.ScaleLevels.Count > 0)
        {
            var scale = Selectable.SelectedSelectable.CurrentPreviewScaleLevel.Size;
            TextLength.text = $"{scale * 1000f} mm";
        }
    }
}
