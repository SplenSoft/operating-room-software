using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public static EventHandler Selected;
    public static Selectable SelectedSelectable { get; private set; }
    public bool IsSelected => SelectedSelectable == this;

    private readonly float _maxTimeMouseDownForSelect = 0.250f;
    private float _timeMouseDown;
    
    [field: SerializeField] private HighlightEffect HighlightEffect { get; set; }

    private void OnMouseDown()
    {
        _timeMouseDown = 0f;
    }

    private void OnMouseDrag()
    {
        _timeMouseDown += Time.deltaTime;
    }

    private void OnMouseUp()
    {
        if (_timeMouseDown < _maxTimeMouseDownForSelect)
        {
            Select();
        }
    }

    private void Deselect()
    {
        HighlightEffect.highlighted = false;
    }

    private void Select()
    {
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect();
        }
        SelectedSelectable = this;
        HighlightEffect.highlighted = true;
        Selected?.Invoke(this, null);
    }
}
