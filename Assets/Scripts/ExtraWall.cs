using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExtraWall : MonoBehaviour
{
    public static UnityEvent ExtraWallSelectionChanged { get; } = new();
    public static ExtraWall SelectedExtraWall => SelectedExtraWalls.Count > 0 ? SelectedExtraWalls[0] : null;
    private static List<ExtraWall> SelectedExtraWalls { get; } = new();
    private bool _isActive;
    [field: SerializeField] Selectable Selectable { get; set; }


    private void Awake()
    {
        Selectable.SelectionChanged += SelectableChanged;
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= SelectableChanged;
        SelectedExtraWalls.Remove(this);
    }

    private void SelectableChanged(object sender, EventArgs e)
    {
        SetActive(Selectable.SelectedSelectable == Selectable);
    }

    private void SetActive(bool active)
    {
        if (_isActive != active) 
        {
            _isActive = active;
            if (_isActive) 
            {
                SelectedExtraWalls.Add(this);
            }
            else
            {
                SelectedExtraWalls.Remove(this);
            }
            ExtraWallSelectionChanged?.Invoke();
        }
    }
}
