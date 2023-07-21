using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonExportObj : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += (o, e) =>
        {
            gameObject.SetActive(Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.IsArmAssembly());
        };

        gameObject.SetActive(false);
    }

    public void ExportObj()
    {
        if (Selectable.SelectedSelectable.TryGetArmAssemblyRoot(out GameObject obj))
        {
            ObjExporter.DoExport(true, obj);
        }
        else
        {
            throw new System.Exception("Something went wrong. Couldn't export OBJ");
        }
    }
}
