using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public EventHandler MouseOverStateChanged;
    public static EventHandler SelectionChanged;
    public static Selectable SelectedSelectable { get; private set; }
    public bool IsSelected => SelectedSelectable == this;
    private bool _isRaycastPlacementMode;
    private bool _hasBeenPlaced;
    private Transform _virtualParent;
    public bool IsMouseOver { get; private set; }
    private GizmoHandler _gizmoHandler;

    [field: SerializeField] private HighlightEffect HighlightEffect { get; set; }
    [field: SerializeField] public Sprite Thumbnail { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] private List<RoomBoundaryType> WallRestrictions { get; set; } = new();

    public AttachmentPoint ParentAttachmentPoint { get; set; }

    private void Awake()
    {
        _gizmoHandler = GetComponent<GizmoHandler>();
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;
    }

    private void OnDestroy()
    {
        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
        if (ParentAttachmentPoint != null)
        {
            ParentAttachmentPoint.DetachSelectable();
        }
    }

    public void OnMouseUpAsButton()
    {
        Select();
    }

    private void OnMouseEnter()
    {
        IsMouseOver = true;
        MouseOverStateChanged?.Invoke(this, null);
    }

    private void OnMouseExit()
    {
        IsMouseOver = false;
        MouseOverStateChanged?.Invoke(this, null);
    }

    public async void StartRaycastPlacementMode()
    {
        if (ParentAttachmentPoint != null) return;
        DeselectAll();
        HighlightEffect.highlighted = true;
        await Task.Yield();
        if (!Application.isPlaying) return;

        _isRaycastPlacementMode = true;
    }

    private void Update()
    {
        UpdateRaycastPlacementMode();
    }

    private async void UpdateRaycastPlacementMode()
    {
        if (!_isRaycastPlacementMode || _hasBeenPlaced) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, 1 << LayerMask.NameToLayer("Wall")))
        {
            void SetPosition(RaycastHit hit)
            {
                transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                _virtualParent = hit.collider.transform;
            }

            if (WallRestrictions.Count > 0 && _virtualParent == null)
            {
                if (WallRestrictions[0] == RoomBoundaryType.Ceiling)
                {
                    var ray2 = new Ray(Vector3.zero + Vector3.up, Vector3.up);
                    if (Physics.Raycast(ray2, out RaycastHit raycastHit2, 100f, 1 << LayerMask.NameToLayer("Wall")))
                    {
                        SetPosition(raycastHit2);
                    }
                }
                else if (WallRestrictions[0] == RoomBoundaryType.Floor)
                {
                    var ray2 = new Ray(Vector3.zero + Vector3.up, -Vector3.up);
                    if (Physics.Raycast(ray2, out RaycastHit raycastHit2, 100f, 1 << LayerMask.NameToLayer("Wall")))
                    {
                        SetPosition(raycastHit2);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                
            }
            else if (WallRestrictions.Count > 0) 
            {
                var wall = raycastHit.collider.GetComponent<RoomBoundary>();
                if (WallRestrictions.Contains(wall.RoomBoundaryType))
                {
                    SetPosition(raycastHit);
                }
            }
            else
            {
                SetPosition(raycastHit);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            HighlightEffect.highlighted = false;
            _hasBeenPlaced = true;
            SendMessage("SelectablePositionChanged");
            SendMessage("VirtualParentChanged", _virtualParent);
            await Task.Yield();
            if (!Application.isPlaying) return;

            _isRaycastPlacementMode = false;
        }
    }

    private void InputHandler_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
    {
        if (e.KeyCode == KeyCode.Escape && e.KeyState == KeyState.ReleasedThisFrame)
        {
            Deselect();

            if (_isRaycastPlacementMode)
            {
                Destroy(gameObject);
            }
        }
        else if (e.KeyCode == KeyCode.Delete && e.KeyState == KeyState.ReleasedThisFrame && IsSelected)
        {
            Deselect();
            Destroy(gameObject);
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
        if (IsSelected || _isRaycastPlacementMode) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect();
        }
        SelectedSelectable = this;
        HighlightEffect.highlighted = true;
        SelectionChanged?.Invoke(this, null);

        if (ParentAttachmentPoint == null)
            _gizmoHandler.SelectableSelected();
    }
}
