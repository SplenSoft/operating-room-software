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
    [field: SerializeField] public List<Selectable> AttachedSelectable { get; private set; } = new(0);

    [SerializeField, ReadOnly] private bool _attachmentPointHovered;
    [field: SerializeField] private HighlightEffect HighlightHovered { get; set; }
    [field: SerializeField] public List<SelectableType> AllowedSelectableTypes { get; private set; } = new();
    [field: SerializeField] public List<Selectable> AllowedSelectables { get; private set; } = new();
    /// <summary>
    /// Moves up in transform hierarchy to same parent as first parent attachment point. Used to keep rotations separate for multiple arm assemblies
    /// </summary>
    [field: SerializeField] private bool MoveUpOnAttach { get; set; }
    /// <summary>
    /// Lower transform hierarchy items will use this attachment point as a rotation reference when taking elevation photos (instead of using ceiling mount attachment points). This is used for arm segments having opposite rotation directions in elevation photos.
    /// </summary>
    [field: SerializeField] public bool TreatAsTopMost { get; private set; }
    /// <summary>
    /// Allows the attachment point to have multiple attached selectables, otherwise attachpoint will disable once an attachment is selected
    /// </summary>
    [field: SerializeField] private bool MultiAttach { get; set; }
    /// <summary>
    /// Sets the maximum number of attachments for a attachment points with MultiAttach set to True
    /// </summary>
    [field: SerializeField] private int MultiLimit { get; set; } = 3;

    public List<Selectable> ParentSelectables { get; } = new();
    [SerializeField, ReadOnly] private Transform _originalParent;
    private MeshRenderer _renderer;
    private Collider _collider;
    private bool _isDestroyed;

    public void SetAttachedSelectable(Selectable selectable)
    {
        AttachedSelectable.Add(selectable);
        if (MoveUpOnAttach)
        {
            Transform parent = transform.parent;
            AttachmentPoint attachmentPoint = parent.GetComponent<AttachmentPoint>();
            while (attachmentPoint == null)
            {
                parent = parent.parent;
                attachmentPoint = parent.GetComponent<AttachmentPoint>();
            }

            transform.parent = attachmentPoint.transform.parent;
        }

        EndHoverStateIfHovered();
        UpdateComponentStatus();
    }

    public void DetachSelectable(Selectable selectable)
    {
        if (_isDestroyed) return;
        AttachedSelectable.Remove(selectable);
        AttachedSelectable.TrimExcess();
        if (MoveUpOnAttach)
        {
            transform.parent = _originalParent;
        }
        UpdateComponentStatus();
    }

    private void Awake()
    {
        EmptyNullList();

        _collider = GetComponentInChildren<Collider>();
        _renderer = GetComponentInChildren<MeshRenderer>();

        Transform parent = transform.parent;
        _originalParent = parent;

        while (parent.GetComponent<AttachmentPoint>() == null)
        {
            var selectable = parent.GetComponent<Selectable>();
            if (selectable != null && !ParentSelectables.Contains(selectable))
            {
                ParentSelectables.Add(selectable);
                selectable.SelectableDestroyed.AddListener(() => Destroy(gameObject));
            }
            parent = parent.parent;
            if (parent == null) break;
        }

        var childSelectables = GetComponentsInChildren<Selectable>();
        if (childSelectables.Length > 0 && AttachedSelectable.Count == 0)
        {
            foreach (Selectable s in childSelectables)
            {
                SetAttachedSelectable(s);
            }
        }

        ParentSelectables.ForEach(item =>
        {
            item.MouseOverStateChanged += MouseOverStateChanged;
        });

        Selectable.SelectionChanged += SelectionChanged;
        ObjectMenu.ActiveStateChanged.AddListener(EndHoverStateIfHovered);
        UpdateComponentStatus();
    }

    private void EndHoverState()
    {
        HoveredAttachmentPoint = null;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = false;
        UpdateComponentStatus();
    }

    private void EndHoverStateIfHovered()
    {
        if (HoveredAttachmentPoint == this)
        {
            EndHoverState();
        }
    }

    private void SelectionChanged(object sender, EventArgs e)
    {
        if (ParentSelectables.Contains(Selectable.SelectedSelectable))
            UpdateComponentStatus();
    }

    private void MouseOverStateChanged(object sender, EventArgs e)
    {
        UpdateComponentStatus();
    }

    private void UpdateComponentStatus()
    {
        int multiAllowed = MultiAttach ? MultiLimit : 0; // check if this is a multiattach point and use the limit, otherwise use 0 for default points. 

        bool isMouseOverAnyParentSelectable = ParentSelectables.FirstOrDefault(item => item.IsMouseOver) != default;
        bool areAnyParentSelectablesSelected = ParentSelectables.Contains(Selectable.SelectedSelectable);
        _renderer.enabled = (isMouseOverAnyParentSelectable || _attachmentPointHovered) && !areAnyParentSelectablesSelected && AttachedSelectable.Count <= multiAllowed;
        HighlightHovered.highlighted = _attachmentPointHovered && !areAnyParentSelectablesSelected && AttachedSelectable.Count <= multiAllowed;
        _collider.enabled = AttachedSelectable.Count <= multiAllowed && !areAnyParentSelectablesSelected;
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
        ObjectMenu.ActiveStateChanged.RemoveListener(EndHoverStateIfHovered);
        ParentSelectables.ForEach(item =>
        {
            item.MouseOverStateChanged -= MouseOverStateChanged;
        });
        Selectable.SelectionChanged -= SelectionChanged;
    }

    private void OnMouseEnter()
    {
        if (GizmoHandler.GizmoBeingUsed || InputHandler.IsPointerOverUIElement()) return;
        HoveredAttachmentPoint = this;
        AttachmentPointHoverStateChanged?.Invoke(this, EventArgs.Empty);
        _attachmentPointHovered = true;
        UpdateComponentStatus();
    }

    private void OnMouseExit()
    {
        if (GizmoHandler.GizmoBeingUsed || InputHandler.IsPointerOverUIElement()) return;
        EndHoverState();
    }

    private void OnMouseUpAsButton()
    {
        if (GizmoHandler.GizmoBeingUsed || InputHandler.IsPointerOverUIElement()) return;
        AttachmentPointClicked?.Invoke(this, EventArgs.Empty);
        SelectedAttachmentPoint = this;
        UpdateComponentStatus();
        ObjectMenu.Open(this);
    }

    private void EmptyNullList()
    {
        if (AttachedSelectable.Count == 1)
        {
            if (AttachedSelectable[0] == null)
            {
                AttachedSelectable.Clear();
                AttachedSelectable.TrimExcess();
            }
        }
    }
}
