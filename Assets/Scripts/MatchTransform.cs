using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Meant to be added to a root object in the scene and will
/// match the scale (on the designated axes), position and 
/// rotation of a selectable 
/// </summary>
public class MatchTransform : MonoBehaviour
{
    [field: SerializeField] 
    public Selectable SelectableToMatch { get; set; }

    /// <summary>
    /// Will match the scale on these (local) axes only
    /// </summary>
    [field: SerializeField, 
    Tooltip("Will match the scale on these (local) " +
    "axes only")] 
    private List<Axis> ScaleAxes { get; set; } = new();

    /// <summary>
    /// Additional scale to after scaling to match the 
    /// designated Selectable
    /// </summary>
    [field: SerializeField, 
    Tooltip("Additional scale to add (to local axes) after " +
    "scaling to match the designated Selectable")] 
    private Vector3 AdditionalScale { get; set; }

    /// <summary>
    /// <see cref="GizmoHandler"/> component on the same 
    /// object as <see cref="SelectableToMatch"/>. 
    /// Assigned on <see cref="Start"/>
    /// </summary>
    private GizmoHandler _gizmoHandler;

    /// <summary>
    /// <see cref="RoomBoundary"/> component on the same 
    /// object as <see cref="SelectableToMatch"/>. 
    /// Assigned on <see cref="Start"/>, can be null
    /// </summary>
    private RoomBoundary _roomBoundary;

    private MoveToRootOnStart _moveToRootOnStart;

    private UnityEventManager _eventManager = new();

    private void Awake()
    {
        _gizmoHandler = SelectableToMatch.gameObject
            .GetComponent<GizmoHandler>();

        _roomBoundary = SelectableToMatch
            .GetComponent<RoomBoundary>();

        _moveToRootOnStart = GetComponent<MoveToRootOnStart>();

        _eventManager.RegisterEvents
            ((_gizmoHandler.GizmoDragPostUpdate, UpdateTransform),
            (_gizmoHandler.GizmoDragEnded, UpdateTransform),
            (SelectableToMatch.OnRaycastPositionUpdated, UpdateTransform), 
            (SelectableToMatch.OnPlaced, UpdateTransform));

        if (_roomBoundary != null)
        {
            _eventManager.RegisterEvent
                (_roomBoundary.SizeSet, UpdateTransform);
        }

        if (_moveToRootOnStart != null) 
        { 
            _eventManager.RegisterEvent
                (_moveToRootOnStart.OnMoved, UpdateTransform);
        }

        _eventManager.AddListeners();
    }

    private void Start()
    {
        UpdateTransform();
    }

    private void OnDestroy()
    {
        _eventManager.RemoveListeners();
    }

    private void UpdateTransform()
    {
        Transform transToMatch = SelectableToMatch.transform;

        // Match position and rotation
        transform.SetPositionAndRotation
            (transToMatch.position, transToMatch.rotation);

        MatchScale();
    }

    private void MatchScale()
    {
        Transform transToMatch = SelectableToMatch.transform;
        Vector3 scaleToMatch = transToMatch.localScale;
        Vector3 newScale;

        newScale.x = ScaleAxes.Contains(Axis.X) ?
            scaleToMatch.x + AdditionalScale.x :
            transform.localScale.x;

        newScale.y = ScaleAxes.Contains(Axis.Y) ?
            scaleToMatch.y + AdditionalScale.y :
            transform.localScale.y;

        newScale.z = ScaleAxes.Contains(Axis.Z) ?
            scaleToMatch.z + AdditionalScale.z :
            transform.localScale.z;

        transform.localScale = newScale;
    }
}
