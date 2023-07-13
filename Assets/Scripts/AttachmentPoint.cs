using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    public static AttachmentPoint HoveredAttachmentPoint { get; private set; }
    public static AttachmentPoint SelectedAttachmentPoint { get; private set; }
    public static EventHandler AttachmentPointHoverStateChanged;
    public static EventHandler AttachmentPointClicked;
    [field: SerializeField] public Selectable AttachedSelectable { get; private set; }

    [SerializeField, ReadOnly] private bool _attachmentPointHovered;
    [field: SerializeField] private HighlightEffect HighlightHovered { get; set; }
    [field: SerializeField] public List<SelectableType> AllowedSelectableTypes { get; private set; } = new();
    [field: SerializeField] public List<Selectable> AllowedSelectables { get; private set; } = new();

    private List<Selectable> _parentSelectables = new();
    private MeshRenderer _renderer;
    private Collider _collider;

    public void SetAttachedSelectable(Selectable selectable)
    {
        AttachedSelectable = selectable;
        UpdateComponentStatus();
    }

    public void DetachSelectable() 
    {
        AttachedSelectable = null;
        UpdateComponentStatus();
    }

    private void Awake()
    {
        Transform parent = transform.parent;

        while (parent.GetComponent<AttachmentPoint>() == null)
        {
            var selectable = parent.GetComponent<Selectable>();
            if (selectable != null && !_parentSelectables.Contains(selectable))
            {
                _parentSelectables.Add(selectable);
            }
            parent = parent.parent;
            if (parent == null) break;
        }

        _collider = GetComponentInChildren<Collider>();
        _renderer = GetComponentInChildren<MeshRenderer>();
        _parentSelectables.ForEach(item =>
        {
            item.MouseOverStateChanged += MouseOverStateChanged;
        });

        Selectable.SelectionChanged += SelectionChanged;
        UpdateComponentStatus();
    }

    private void SelectionChanged(object sender, EventArgs e)
    {
        if (_parentSelectables.Contains(Selectable.SelectedSelectable))
            UpdateComponentStatus();
    }

    private void MouseOverStateChanged(object sender, EventArgs e)
    {
        UpdateComponentStatus();
    }

    private void UpdateComponentStatus()
    {
        bool isMouseOverAnyParentSelectable = _parentSelectables.FirstOrDefault(item => item.IsMouseOver) != default;
        bool areAnyParentSelectablesSelected = _parentSelectables.Contains(Selectable.SelectedSelectable);
        _renderer.enabled = (isMouseOverAnyParentSelectable || _attachmentPointHovered) && !areAnyParentSelectablesSelected && AttachedSelectable == null;
        HighlightHovered.highlighted = _attachmentPointHovered && !areAnyParentSelectablesSelected && AttachedSelectable == null;
        _collider.enabled = AttachedSelectable == null && !areAnyParentSelectablesSelected;
    }

    private void OnDestroy()
    {
        _parentSelectables.ForEach(item =>
        {
            item.MouseOverStateChanged -= MouseOverStateChanged;
        });
        Selectable.SelectionChanged -= SelectionChanged;
    }

    private void OnMouseEnter()
    {
        if (GizmoHandler.GizmoBeingUsed) return;
        HoveredAttachmentPoint = this;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = true;
        UpdateComponentStatus();
    }

    private void OnMouseExit()
    {
        if (GizmoHandler.GizmoBeingUsed) return;
        HoveredAttachmentPoint = null;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = false;
        UpdateComponentStatus();
    }

    private void OnMouseUpAsButton()
    {
        if (GizmoHandler.GizmoBeingUsed) return;
        AttachmentPointClicked?.Invoke(this, EventArgs.Empty);
        SelectedAttachmentPoint = this;
        UpdateComponentStatus();
        ObjectMenu.Open(this);
    }
}
