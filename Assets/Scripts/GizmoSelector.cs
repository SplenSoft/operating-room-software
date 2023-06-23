using System;
using TMPro;
using UnityEngine;

public class GizmoSelector : MonoBehaviour
{
    public static GizmoMode CurrentGizmoMode { get; private set; }
    public static EventHandler GizmoModeChanged;
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
        _dropdown.onValueChanged.AddListener(selection => SetGizmoMode((GizmoMode)selection));
        gameObject.SetActive(false);
        Selectable.SelectionChanged += (o, e) => gameObject.SetActive(Selectable.SelectedSelectable != null);
    }

    public void SetGizmoMode(GizmoMode gizmoMode)
    {
        bool isDifferent = gizmoMode != CurrentGizmoMode;
        CurrentGizmoMode = gizmoMode;
        if (isDifferent) 
        { 
            GizmoModeChanged?.Invoke(this, null);
        }
    }
}