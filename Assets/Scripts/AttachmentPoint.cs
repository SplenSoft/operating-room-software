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

    [SerializeField, ReadOnly] private bool _attachmentPointHovered;
    [field: SerializeField] private HighlightEffect HighlightHovered { get; set; }

    private MeshRenderer _renderer;

    [field: SerializeField] private Selectable Selectable { get; set; }

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        Selectable.MouseOverStateChanged += MouseOverStateChanged;
        Selectable.SelectionChanged += SelectionChanged;
        CheckVisibility();
    }

    private void SelectionChanged(object sender, EventArgs e)
    {
        if (Selectable.SelectedSelectable == Selectable)
            CheckVisibility();
    }

    private void MouseOverStateChanged(object sender, EventArgs e)
    {
        CheckVisibility();
    }

    private void CheckVisibility()
    {
        _renderer.enabled = (Selectable.IsMouseOver || _attachmentPointHovered) && !Selectable.IsSelected;
        HighlightHovered.highlighted = _attachmentPointHovered && !Selectable.IsSelected;
    }

    private void OnDestroy()
    {
        Selectable.MouseOverStateChanged -= MouseOverStateChanged;
    }

    private void OnMouseEnter()
    {
        HoveredAttachmentPoint = this;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = true;
        CheckVisibility();
    }

    private void OnMouseExit()
    {
        HoveredAttachmentPoint = null;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = false;
        CheckVisibility();
    }

    private void OnMouseUpAsButton()
    {
        AttachmentPointClicked?.Invoke(this, EventArgs.Empty);
        SelectedAttachmentPoint = this;
        CheckVisibility();
    }
}
