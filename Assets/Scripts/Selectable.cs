using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public static EventHandler SelectionChanged;
    public static Selectable SelectedSelectable { get; private set; }
    public bool IsSelected => SelectedSelectable == this;
    
    [field: SerializeField] private HighlightEffect HighlightEffect { get; set; }

    private void Awake()
    {
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;
    }

    private void OnDestroy()
    {
        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
    }

    public void OnMouseUpAsButton()
    {
        Select();
    }

    private void InputHandler_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
    {
        if (e.KeyCode == KeyCode.Escape && e.KeyState == KeyState.ReleasedThisFrame)
        {
            Deselect();
        }
    }

    public static void DeselectAll()
    {
        if (SelectedSelectable != null)
        {
            if (SelectedSelectable.GetComponent<GizmoHandler>().GizmoUsedLastFrame) return;
            SelectedSelectable.Deselect();
            SelectionChanged?.Invoke(null, null);
        }
    }

    private void Deselect()
    {
        if (!IsSelected) return;
        SelectedSelectable = null;
        HighlightEffect.highlighted = false;
        SendMessage("SelectableDeselected");
    }

    private void Select()
    {
        if (IsSelected) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect();
        }
        SelectedSelectable = this;
        HighlightEffect.highlighted = true;
        SelectionChanged?.Invoke(this, null);
        SendMessage("SelectableSelected");
    }
}
