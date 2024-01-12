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

    private void OnSelectionChanged(object sender, EventArgs e)
    {
        gameObject.SetActive(Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.IsArmAssembly);
    }

    public void ExportPdf()
    {
        if (Selectable.SelectedSelectable.TryGetArmAssemblyRoot(out _))
        {
            Selectable.SelectedSelectable.ExportElevationPdf();
        }
        else
        {
            throw new System.Exception("Something went wrong. Couldn't export PDF");
        }
    }
}
