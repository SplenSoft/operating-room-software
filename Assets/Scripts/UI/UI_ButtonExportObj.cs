using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonExportObj : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += UpdateActiveState;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= UpdateActiveState;
    }

    private void UpdateActiveState()
    {
        gameObject.SetActive(Selectable.SelectedSelectables.Count > 0 && Selectable.SelectedSelectables[0].IsArmAssembly);
    }

    public void ExportObj()
    {
        if (Selectable.SelectedSelectables[0].TryGetArmAssemblyRoot(out GameObject obj))
        {
            ObjExporter.DoExport(true, obj);
        }
        else
        {
            throw new System.Exception("Something went wrong. Couldn't export OBJ");
        }
    }
}
