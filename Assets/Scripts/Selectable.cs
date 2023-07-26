using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.UI;
using static UnityEditor.Progress;

[RequireComponent(typeof(GizmoHandler), typeof(HighlightEffect))]
public class Selectable : MonoBehaviour
{
    [Serializable]
    public class ScaleLevel
    {
        [field: SerializeField] public float Size { get; set; }
        [field: SerializeField] public bool Selected { get; set; }
        public float ScaleZ { get; set; }
    }

    #region Fields and Properties
    public EventHandler MouseOverStateChanged;
    public static EventHandler SelectionChanged;
    public UnityEvent SelectableDestroyed { get; } = new();

    public bool IsMouseOver { get; private set; }
    public static Selectable SelectedSelectable { get; private set; }
    public bool IsDestroyed { get; private set; }

    public Dictionary<GizmoType, Dictionary<Axis, GizmoSetting>> GizmoSettings { get; } = new();
    public Vector3 OriginalLocalPosition { get; private set; }
    public Vector3 OriginalLocalRotation { get; private set; }

    [field: SerializeField] public AttachmentPoint ParentAttachmentPoint { get; set; }
    [field: SerializeField] public Sprite Thumbnail { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] private List<RoomBoundaryType> WallRestrictions { get; set; } = new();
    [field: SerializeField] public List<SelectableType> Types { get; private set; } = new();
    [field: SerializeField] private Vector3 InitialLocalPositionOffset { get; set; }
    [field: SerializeField] private List<GizmoSetting> GizmoSettingsList { get; set; } = new();
    [field: SerializeField] public List<ScaleLevel> ScaleLevels { get; private set; } = new();
    [field: SerializeField] private bool ZAlwaysFacesGround { get; set; }
    [field: SerializeField] public Measurable Measurable { get; private set; }
    [field: SerializeField] private bool AlignForElevationPhoto { get; set; }

    private List<Vector3> _childScales = new();
    private Quaternion _originalRotation;
    private Selectable _parentSelectable;
    private Transform _virtualParent;
    private HighlightEffect _highlightEffect;
    private GizmoHandler _gizmoHandler;
    private Quaternion _localRotationBeforeElevationPhoto;
    private Quaternion _originalRotation2;

    [SerializeField, ReadOnly] private ScaleLevel _currentScaleLevel;
    [SerializeField, ReadOnly] private ScaleLevel _currentPreviewScaleLevel;   
    
    private bool _isRaycastPlacementMode;
    private bool _hasBeenPlaced;
    #endregion

    public static void DeselectAll()
    {
        if (SelectedSelectable != null)
        {
            if (SelectedSelectable.GetComponent<GizmoHandler>().GizmoUsedLastFrame) return;
            SelectedSelectable.Deselect();
            //SelectionChanged?.Invoke(null, null);
        }
    }

    public bool IsSelected => SelectedSelectable == this;

    private bool CheckConstraints(float currentVal, float originalVal, float maxVal, float minVal, out float excess)
    {
        //if (maxVal == 0 && minVal == 0) return false;
        float diff = currentVal - originalVal;
        excess = diff > maxVal ? diff - maxVal : diff < minVal ? diff - minVal : 0f;
        return excess != 0;
    }

    public bool ExeedsMaxTranslation(out Vector3 totalExcess)
    {
        Vector3 adjustedTransform = transform.localRotation * transform.localPosition;

        float maxTranslationX = GetGizmoSettingMaxValue(GizmoType.Move, Axis.X) * transform.localScale.x;
        float maxTranslationY = GetGizmoSettingMaxValue(GizmoType.Move, Axis.Y) * transform.localScale.y;
        float maxTranslationZ = GetGizmoSettingMaxValue(GizmoType.Move, Axis.Z) * transform.localScale.z;

        float minTranslationX = GetGizmoSettingMinValue(GizmoType.Move, Axis.X) * transform.localScale.x;
        float minTranslationY = GetGizmoSettingMinValue(GizmoType.Move, Axis.Y) * transform.localScale.y;
        float minTranslationZ = GetGizmoSettingMinValue(GizmoType.Move, Axis.Z) * transform.localScale.z;

        Vector3 adjustedMaxTranslation = new Vector3(maxTranslationX, maxTranslationY, maxTranslationZ);
        Vector3 adjustedMinTranslation = new Vector3(minTranslationX, minTranslationY, minTranslationZ);
        totalExcess = default;
        bool exceedsX = IsGizmoSettingAllowed(GizmoType.Move, Axis.X) && CheckConstraints(adjustedTransform.x, OriginalLocalPosition.x, adjustedMaxTranslation.x, adjustedMinTranslation.x, out totalExcess.x);
        bool exceedsY = IsGizmoSettingAllowed(GizmoType.Move, Axis.Y) && CheckConstraints(adjustedTransform.y, OriginalLocalPosition.y, adjustedMaxTranslation.y, adjustedMinTranslation.y, out totalExcess.y);
        bool exceedsZ = IsGizmoSettingAllowed(GizmoType.Move, Axis.Z) && CheckConstraints(adjustedTransform.z, OriginalLocalPosition.z, adjustedMaxTranslation.z, adjustedMinTranslation.z, out totalExcess.z);
        return exceedsX || exceedsY || exceedsZ;
    }

    /// <returns>True if any rotation happened</returns>
    public bool TryRotateTowardVector(Vector3 directionVector)
    {
        if (IsGizmoSettingAllowed(GizmoType.Rotate, Axis.X) || IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Y) || IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z))
        {
            Quaternion oldRotation = transform.localRotation;
            transform.LookAt(transform.position + directionVector);
            if (ExceedsMaxRotation(out Vector3 totalExcess))
            {
                transform.localRotation *= Quaternion.Euler(-totalExcess.x, -totalExcess.y, -totalExcess.z);
            }

            if (oldRotation != transform.localRotation) return true;
        }

        return false;
    }

    public bool ExceedsMaxRotation(out Vector3 totalExcess)
    {
        float angleX = transform.localEulerAngles.x > 180 ? transform.localEulerAngles.x - 360f : transform.localEulerAngles.x;
        float angleY = transform.localEulerAngles.y > 180 ? transform.localEulerAngles.y - 360f : transform.localEulerAngles.y;
        float angleZ = transform.localEulerAngles.z > 180 ? transform.localEulerAngles.z - 360f : transform.localEulerAngles.z;
        totalExcess = default;
        bool exceedsX = IsGizmoSettingAllowed(GizmoType.Rotate, Axis.X) && CheckConstraints(angleX, OriginalLocalRotation.x, GetGizmoSettingMaxValue(GizmoType.Rotate, Axis.X), GetGizmoSettingMinValue(GizmoType.Rotate, Axis.X), out totalExcess.x);
        bool exceedsY = TryGetGizmoSetting(GizmoType.Rotate, Axis.Y, out _) && CheckConstraints(angleY, OriginalLocalRotation.y, GetGizmoSettingMaxValue(GizmoType.Rotate, Axis.Y), GetGizmoSettingMinValue(GizmoType.Rotate, Axis.Y), out totalExcess.y);
        bool exceedsZ = TryGetGizmoSetting(GizmoType.Rotate, Axis.Z, out _) && CheckConstraints(angleZ, OriginalLocalRotation.z, GetGizmoSettingMaxValue(GizmoType.Rotate, Axis.Z), GetGizmoSettingMinValue(GizmoType.Rotate, Axis.Z), out totalExcess.z);
        return exceedsX || exceedsY || exceedsZ;
    }

    public bool IsArmAssembly()
    {
        var rootSelectable = transform.root.GetComponent<Selectable>();
        return rootSelectable != null && rootSelectable.Types.Contains(SelectableType.Mount);
    }

    public bool TryGetArmAssemblyRoot(out GameObject rootObj)
    {
        rootObj = null;
        if (transform.root.TryGetComponent<Selectable>(out var rootSelectable)) 
        {
            rootObj = rootSelectable.gameObject;
            return rootSelectable.Types.Contains(SelectableType.Mount);
        }
        return false;
    }

    private void UpdateZScaling(bool setSelected)
    {
        if (ScaleLevels.Count == 0) return;
        //get closest scale in list
        ScaleLevel closest = ScaleLevels.OrderBy(item => Math.Abs(_gizmoHandler.CurrentScaleDrag.z - item.ScaleZ)).First();
        if (closest == _currentPreviewScaleLevel && !setSelected) return;

        _currentPreviewScaleLevel = closest;

        Quaternion storedRotation = transform.rotation;
        transform.rotation = _originalRotation;

        for (int j = 0; j < 2; j++) //not sure if still need to do this twice
        {
            Vector3 parentOriginalScale = transform.localScale;
            Vector3 newScale = new Vector3(transform.localScale.x, transform.localScale.y, closest.ScaleZ);
            transform.localScale = newScale;

            if (closest == _currentScaleLevel)
            {
                //Debug.Log("Using stored child scales");
                for (int i = 0; i < transform.childCount; i++)
                    transform.GetChild(i).transform.localScale = _childScales[i];
            }
            else
            {
                //Debug.Log("Calculating child scales");
                Vector3 newParentScale = newScale;
                // Get the relative difference to the original scale
                var diffX = newParentScale.x / parentOriginalScale.x;
                var diffY = newParentScale.y / parentOriginalScale.y;
                var diffZ = newParentScale.z / parentOriginalScale.z;

                // This inverts the scale differences
                var diffVector = new Vector3(1 / diffX, 1 / diffY, 1 / diffZ);

                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    Vector3 localDiff = child.transform.InverseTransformVector(diffVector);
                    float x = Mathf.Abs(child.transform.localScale.x * localDiff.x);
                    float y = Mathf.Abs(child.transform.localScale.y * localDiff.y);
                    float z = Mathf.Abs(child.transform.localScale.z * localDiff.z);
                    child.transform.localScale = new Vector3(x, y, z);
                }
            }
        }

        if (setSelected)
        {
            ScaleLevels.ForEach((item) => item.Selected = false);
            closest.Selected = true;
            _currentScaleLevel = closest;
            StoreChildScales();
        }

        transform.rotation = storedRotation;
    }

    private void StoreChildScales()
    {
        _childScales.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            _childScales.Add(child.transform.localScale);
        }
    }

    public bool TryGetGizmoSetting(GizmoType gizmoType, Axis axis, out GizmoSetting gizmoSetting)
    {
        gizmoSetting = default;
        if (!GizmoSettings.ContainsKey(gizmoType)) return false;
        if (!GizmoSettings[gizmoType].ContainsKey(axis)) return false;
        gizmoSetting = GizmoSettings[gizmoType][axis];
        return true;
    }

    public bool IsGizmoSettingAllowed(GizmoType gizmoType, Axis axis) => TryGetGizmoSetting(gizmoType, axis, out _);

    private float GetGizmoSettingMaxValue(GizmoType gizmoType, Axis axis)
    {
        if (TryGetGizmoSetting(gizmoType, axis, out GizmoSetting gizmoSetting))
        {
            return gizmoSetting.GetMaxValue;
        }
        else return 0;
    }

    private float GetGizmoSettingMinValue(GizmoType gizmoType, Axis axis)
    {
        if (TryGetGizmoSetting(gizmoType, axis, out GizmoSetting gizmoSetting))
        {
            return gizmoSetting.GetMinValue;
        }
        else return 0;
    }

    private int GetParentCount()
    {
        Transform parent = transform.parent;
        int count = 0;
        while (parent != transform.root && parent != null)
        {
            parent = parent.parent;
            count++;
        }

        return count;
    }

    public void SetForElevationPhoto(ElevationPhotoType elevationPhotoType)
    {
        if (TryGetArmAssemblyRoot(out GameObject rootObj))
        {
            if (rootObj == gameObject)
            {
                // this obj is the ceiling mount
                //List<AttachmentPoint> topMostAttachmentPoints = GetComponentsInChildren<AttachmentPoint>().Where(item => item.ParentSelectables.Contains(this)).ToList();
                Array.ForEach(GetComponentsInChildren<Selectable>().OrderBy(x => x.GetParentCount()).ToArray(), item =>
                {
                    if (item.AlignForElevationPhoto)
                    {
                        //AttachmentPoint topMostAttachmentPoint = null;
                        //Transform parent = item.transform.parent;
                        //while (parent != item.transform.root)
                        //{
                        //    if (parent.TryGetComponent(out AttachmentPoint attachmentPoint))
                        //    {
                        //        topMostAttachmentPoint = attachmentPoint;
                        //        if (topMostAttachmentPoint.TreatAsTopMost) break;
                        //    }
                        //    parent = parent.parent;
                        //}

                        //if (topMostAttachmentPoint == null)
                        //{
                        //    throw new Exception("Could not find top most attachment point");
                        //}

                        //Vector3 directionXZ = topMostAttachmentPoint.transform.right;
                        //directionXZ.y = 0;
                        //Vector3 thisXZ = item.transform.right;
                        //thisXZ.y = 0;
                        //float signedAngle = Vector3.SignedAngle(directionXZ, thisXZ, Vector3.down);
                        //item._localRotationBeforeElevationPhoto = item.transform.localRotation;
                        //item.transform.Rotate(new Vector3(0, 0, signedAngle));
                        item.transform.localRotation = item._originalRotation2;

                    }
                });
            }
            else
            {
                rootObj.GetComponent<Selectable>().SetForElevationPhoto(elevationPhotoType);
                return;
            }
        }
    }

    #region Monobehaviour
    private void Awake()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.TryGetComponent<AttachmentPoint>(out var attachmentPoint))
            {
                if (attachmentPoint != ParentAttachmentPoint)
                {
                    ParentAttachmentPoint = attachmentPoint;
                }
                break;
            }

            if (parent.TryGetComponent<Selectable>(out var selectable))
            {
                _parentSelectable = selectable; 
                break;
            }

            parent = parent.parent;
        }

        _originalRotation = transform.rotation;
        //OriginalLocalRotation = transform.localEulerAngles;
        _highlightEffect = GetComponent<HighlightEffect>();
        _gizmoHandler = GetComponent<GizmoHandler>();
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;

        GizmoSettingsList.ForEach(item =>
        {
            if (!GizmoSettings.ContainsKey(item.GizmoType))
            {
                GizmoSettings[item.GizmoType] = new();
            }
            GizmoSettings[item.GizmoType][item.Axis] = item;
        });

        if (IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z))
        {
            _currentScaleLevel = ScaleLevels.First(item => item.Selected);
            _currentPreviewScaleLevel = _currentScaleLevel;
            _currentScaleLevel.ScaleZ = transform.localScale.z;

            StoreChildScales();

            ScaleLevels.ForEach(item =>
            {
                if (!item.Selected)
                {
                    float perc = item.Size / _currentScaleLevel.Size;
                    item.ScaleZ = _currentScaleLevel.ScaleZ * perc;
                }
            });

            _gizmoHandler.GizmoDragEnded.AddListener(() =>
            {
                if (GizmoSelector.CurrentGizmoMode == GizmoMode.Scale)
                {
                    UpdateZScaling(true);
                }
            });

            _gizmoHandler.GizmoDragPostUpdate.AddListener(() =>
            {
                if (GizmoSelector.CurrentGizmoMode == GizmoMode.Scale)
                {
                    UpdateZScaling(false);
                }
            });
        }
    }

    private void OnDestroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        if (SelectedSelectable == this)
        {
            Deselect();
        }

        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
        if (ParentAttachmentPoint != null)
        {
            ParentAttachmentPoint.DetachSelectable();
        }

        if (_parentSelectable != null)
        {
            Destroy(_parentSelectable.gameObject);
        }

        SelectableDestroyed?.Invoke();
    }

    public void OnMouseUpAsButton()
    {
        if (InputHandler.IsPointerOverUIElement()) return;
        Select();
    }

    private void OnMouseEnter()
    {
        if (GizmoHandler.GizmoBeingUsed) return;
        IsMouseOver = true;
        MouseOverStateChanged?.Invoke(this, null);
    }

    private void OnMouseExit()
    {
        if (GizmoHandler.GizmoBeingUsed) return;
        IsMouseOver = false;
        MouseOverStateChanged?.Invoke(this, null);
    }

    private void Start()
    {
        _originalRotation2 = transform.localRotation;
        OriginalLocalPosition = transform.localPosition;
        Vector3 adjustedOffsetVector = new Vector3(InitialLocalPositionOffset.x * transform.localScale.x, InitialLocalPositionOffset.y * transform.localScale.y, InitialLocalPositionOffset.z * transform.localScale.z);
        transform.localPosition += adjustedOffsetVector;
    }

    private void Update()
    {
        UpdateRaycastPlacementMode();

        if (ZAlwaysFacesGround)
        {
            transform.LookAt(transform.position + Vector3.down, transform.parent.forward);
            if (!IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z))
            {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
            }
        }

        //testing
        if (SelectedSelectable == this && Input.GetKeyUp(KeyCode.Space)) 
        {
            SetForElevationPhoto(ElevationPhotoType.Up);
        }
    }
    #endregion

    public async void StartRaycastPlacementMode()
    {
        if (ParentAttachmentPoint != null) return;
        DeselectAll();
        _highlightEffect.highlighted = true;
        await Task.Yield();
        if (!Application.isPlaying) return;

        _isRaycastPlacementMode = true;
    }

    private async void UpdateRaycastPlacementMode()
    {
        if (!_isRaycastPlacementMode || _hasBeenPlaced) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = 1 << LayerMask.NameToLayer("Wall");
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, mask))
        {
            void SetPosition(RaycastHit hit)
            {
                transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                _virtualParent = hit.collider.transform;
            }

            if (WallRestrictions.Count > 0 && _virtualParent == null)
            {
                Vector3 direction = WallRestrictions[0] == RoomBoundaryType.Ceiling ? Vector3.up : WallRestrictions[0] == RoomBoundaryType.Floor ? Vector3.down : Vector3.right;
                var ray2 = new Ray(Vector3.zero + Vector3.up, direction);
                if (Physics.Raycast(ray2, out RaycastHit raycastHit2, float.MaxValue, mask))
                {
                    SetPosition(raycastHit2);
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
            _highlightEffect.highlighted = false;
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

    private void Deselect(bool fireEvent = true)
    {
        if (!IsSelected) return;
        SelectedSelectable = null;
        _highlightEffect.highlighted = false;
        SendMessage("SelectableDeselected");

        if (fireEvent)
            SelectionChanged?.Invoke(this, null);
    }

    private void Select()
    {
        if (IsSelected || _isRaycastPlacementMode || GizmoHandler.GizmoBeingUsed) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect(false);
        }
        SelectedSelectable = this;
        _highlightEffect.highlighted = true;
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
    BoomSegment,
    BoomHead,
    Wall
}

[Serializable]
public class GizmoSetting
{
    [field: SerializeField] public Axis Axis { get; private set; }
    [field: SerializeField] public GizmoType GizmoType { get; private set; }
    [field: SerializeField] public bool Unrestricted { get; private set; } = true;
    [field: SerializeField] private float MaxValue { get; set; }
    [field: SerializeField] private float MinValue { get; set; }
    public float GetMaxValue => Unrestricted ? float.MaxValue : MaxValue;
    public float GetMinValue => Unrestricted ? float.MinValue : MinValue;
}

public enum Axis
{
    X,
    Y,
    Z
}

public enum GizmoType
{
    Move,
    Rotate,
    Scale
}

public enum ElevationPhotoType
{
    Up,
    Down
}