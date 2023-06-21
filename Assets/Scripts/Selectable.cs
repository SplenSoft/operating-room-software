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

    private void Awake()
    {
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;
    }

    private void OnDestroy()
    {
        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
    }

    public void OnMouseDown()
    {
        _timeMouseDown = 0f;
        Debug.Log("OnMouseDown received");
    }

    public void OnMouseDrag()
    {
        _timeMouseDown += Time.deltaTime;
    }

    public void OnMouseUp()
    {
        Debug.Log("OnMouseUp received");
        if (_timeMouseDown < _maxTimeMouseDownForSelect)
        {
            Select();
        }
    }

    private void InputHandler_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
    {
        if (e.KeyCode == KeyCode.Escape && e.KeyState == KeyState.ReleasedThisFrame)
        {
            Deselect();
        }
    }

    private void Deselect()
    {
        Debug.Log("DeSelecting");
        if (!IsSelected) return;
        SelectedSelectable = null;
        HighlightEffect.highlighted = false;
        Debug.Log("DeSelected");
    }

    private void Select()
    {
        Debug.Log("Selecting");
        if (IsSelected) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect();
        }
        SelectedSelectable = this;
        HighlightEffect.highlighted = true;
        Selected?.Invoke(this, null);
        Debug.Log("Selected");
    }
}
