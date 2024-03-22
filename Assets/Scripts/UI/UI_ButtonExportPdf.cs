using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonExportPdf : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += OnSelectionChanged;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        gameObject.SetActive(Selectable.SelectedSelectables.Count > 0 && 
            Selectable.SelectedSelectables[0].IsArmAssembly);
    }

    public void ExportPdf()
    {
        UI_PdfExportOptions.Open(Selectable.SelectedSelectables[0]);
    }
}
