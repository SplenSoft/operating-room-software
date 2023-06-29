using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(GizmoHandler), typeof(HighlightEffect))]
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
    public bool IsDestroyed { get; private set; }
    public AttachmentPoint ParentAttachmentPoint { get; set; }

    [field: SerializeField, ReadOnly] public Vector3 OriginalLocalPosition { get; private set; }
    [field: SerializeField, ReadOnly] public Vector3 OriginalLocalRotation { get; private set; }

    [field: SerializeField] private HighlightEffect HighlightEffect { get; set; }
    [field: SerializeField] public Sprite Thumbnail { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] private List<RoomBoundaryType> WallRestrictions { get; set; } = new();
    [field: SerializeField] public List<SelectableType> Types { get; private set; } = new();
    [field: SerializeField] public Vector3 MaxLocalTranslation { get; private set; }
    [field: SerializeField] public Vector3 MinLocalTranslation { get; private set; }
    [field: SerializeField] public Vector3 MaxLocalRotation { get; private set; }
    [field: SerializeField] public Vector3 MinLocalRotation { get; private set; }
    [field: SerializeField] private Vector3 InitialLocalPositionOffset { get; set; }
    [field: SerializeField] public bool AllowMovementX { get; set; } = true;
    [field: SerializeField] public bool AllowMovementY { get; set; } = true;
    [field: SerializeField] public bool AllowMovementZ { get; set; } = true;
    [field: SerializeField] public bool AllowRotationX { get; set; } = true;
    [field: SerializeField] public bool AllowRotationY { get; set; } = true;
    [field: SerializeField] public bool AllowRotationZ { get; set; } = true;
    [field: SerializeField] private List<Selectable> Interdependencies { get; set; } = new List<Selectable>();

    private bool CheckConstraints(float currentVal, float originalVal, float maxVal, float minVal, out float excess)
    {
        excess = 0f;
        if (maxVal == 0 && minVal == 0) return false;
        float diff = currentVal - originalVal;
        excess = diff > maxVal ? diff - maxVal : diff < minVal ? diff - minVal : 0f;
        return excess != 0;
    }

    public bool ExeedsMaxTranslation(out Vector3 totalExcess)
    {
        Vector3 adjustedTransform = transform.localRotation * transform.localPosition;
        Vector3 adjustedMaxTranslation = new Vector3(MaxLocalTranslation.x * transform.localScale.x, MaxLocalTranslation.y * transform.localScale.y, MaxLocalTranslation.z * transform.localScale.z);
        Vector3 adjustedMinTranslation = new Vector3(MinLocalTranslation.x * transform.localScale.x, MinLocalTranslation.y * transform.localScale.y, MinLocalTranslation.z * transform.localScale.z);
        totalExcess = default;
        bool exceedsX = AllowMovementX && CheckConstraints(adjustedTransform.x, OriginalLocalPosition.x, adjustedMaxTranslation.x, adjustedMinTranslation.x, out totalExcess.x);
        bool exceedsY = AllowMovementY && CheckConstraints(adjustedTransform.y, OriginalLocalPosition.y, adjustedMaxTranslation.y, adjustedMinTranslation.y, out totalExcess.y);
        bool exceedsZ = AllowMovementZ && CheckConstraints(adjustedTransform.z, OriginalLocalPosition.z, adjustedMaxTranslation.z, adjustedMinTranslation.z, out totalExcess.z);
        return exceedsX || exceedsY || exceedsZ;
    }

    public bool ExceedsMaxRotation(out Vector3 totalExcess)
    {
        float angleX = transform.localEulerAngles.x > 180 ? transform.localEulerAngles.x - 360f : transform.localEulerAngles.x;
        float angleY = transform.localEulerAngles.y > 180 ? transform.localEulerAngles.y - 360f : transform.localEulerAngles.y;
        float angleZ = transform.localEulerAngles.z > 180 ? transform.localEulerAngles.z - 360f : transform.localEulerAngles.z;
        totalExcess = default;
        bool exceedsX = AllowRotationX && CheckConstraints(angleX, OriginalLocalRotation.x, MaxLocalRotation.x, MinLocalRotation.x, out totalExcess.x);
        bool exceedsY = AllowRotationY && CheckConstraints(angleY, OriginalLocalRotation.y, MaxLocalRotation.y, MinLocalRotation.y, out totalExcess.y);
        bool exceedsZ = AllowRotationZ && CheckConstraints(angleZ, OriginalLocalRotation.z, MaxLocalRotation.z, MinLocalRotation.z, out totalExcess.z);
        return exceedsX || exceedsY || exceedsZ;
    }

    private void Awake()
    {
        _gizmoHandler = GetComponent<GizmoHandler>();
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;
    }

    private void Start()
    {
        OriginalLocalPosition = transform.localPosition;
        Vector3 adjustedOffsetVector = new Vector3(InitialLocalPositionOffset.x * transform.localScale.x, InitialLocalPositionOffset.y * transform.localScale.y, InitialLocalPositionOffset.z * transform.localScale.z);
        transform.localPosition += adjustedOffsetVector;
    }

    private void OnDestroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
        if (ParentAttachmentPoint != null)
        {
            ParentAttachmentPoint.DetachSelectable();
        }

        Interdependencies.ForEach(item =>
        {
            Destroy(item.gameObject);
        });
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
        _gizmoHandler.SelectableSelected();
    }
}

public enum SelectableType
{
    DropTube,
    Mount,
    Furniture,
    ArmSegment,
    BoomSegment
}