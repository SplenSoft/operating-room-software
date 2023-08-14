using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonExportPdf : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += (o, e) =>
        {
            gameObject.SetActive(Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.IsArmAssembly());
        };

        gameObject.SetActive(false);
    }

    public void ExportPdf()
    {
        if (Selectable.SelectedSelectable.TryGetArmAssemblyRoot(out GameObject obj))
        {
            Selectable.SelectedSelectable.ExportElevationPdf();
        }
        else
        {
            throw new System.Exception("Something went wrong. Couldn't export PDF");
        }
    }
}
