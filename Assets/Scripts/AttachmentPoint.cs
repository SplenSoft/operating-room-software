using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    public static AttachmentPoint HoveredAttachmentPoint { get; private set; }
    public static AttachmentPoint SelectedAttachmentPoint { get; private set; }
    public static EventHandler AttachmentPointHoverStateChanged;
    public static EventHandler AttachmentPointClicked;
    public Selectable AttachedSelectable { get; private set; }

    [SerializeField, ReadOnly] private bool _attachmentPointHovered;
    [field: SerializeField] private HighlightEffect HighlightHovered { get; set; }

    private MeshRenderer _renderer;
    private Collider _collider;

    [field: SerializeField] private Selectable ParentSelectable { get; set; }

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
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<MeshRenderer>();
        ParentSelectable.MouseOverStateChanged += MouseOverStateChanged;
        Selectable.SelectionChanged += SelectionChanged;
        UpdateComponentStatus();
    }

    private void SelectionChanged(object sender, EventArgs e)
    {
        if (Selectable.SelectedSelectable == ParentSelectable)
            UpdateComponentStatus();
    }

    private void MouseOverStateChanged(object sender, EventArgs e)
    {
        UpdateComponentStatus();
    }

    private void UpdateComponentStatus()
    {
        _renderer.enabled = (ParentSelectable.IsMouseOver || _attachmentPointHovered) && !ParentSelectable.IsSelected && AttachedSelectable == null;
        HighlightHovered.highlighted = _attachmentPointHovered && !ParentSelectable.IsSelected && AttachedSelectable == null;
        _collider.enabled = AttachedSelectable == null && !ParentSelectable.IsSelected;
    }

    private void OnDestroy()
    {
        ParentSelectable.MouseOverStateChanged -= MouseOverStateChanged;
    }

    private void OnMouseEnter()
    {
        HoveredAttachmentPoint = this;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = true;
        UpdateComponentStatus();
    }

    private void OnMouseExit()
    {
        HoveredAttachmentPoint = null;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = false;
        UpdateComponentStatus();
    }

    private void OnMouseUpAsButton()
    {
        AttachmentPointClicked?.Invoke(this, EventArgs.Empty);
        SelectedAttachmentPoint = this;
        UpdateComponentStatus();
        ObjectMenu.Open(this);
    }
}
