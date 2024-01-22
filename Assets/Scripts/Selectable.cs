using HighlightPlus;
using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add basic selectable - <see href="https://youtu.be/qEaRrGC_MX8?si=kCXNSVa11KxLKRNG"/> 
/// </summary>
[RequireComponent(typeof(GizmoHandler), typeof(HighlightEffect), typeof(TrackedObject)), Serializable]
public partial class Selectable : MonoBehaviour
{
    [Serializable]
    public class ScaleLevel
    {
        [field: SerializeField] public float Size { get; set; }
        [field: SerializeField] public bool Selected { get; set; }
        [field: SerializeField] public bool ModelDefault { get; set; }
        public float ScaleZ { get; set; }
        [field: SerializeField] public Metadata[] metadata;

        public bool TryGetValue(string key, out string value)
        {
            value = metadata.SingleOrDefault(x => x.key == key).value;
            return value != "";
        }
    }

    [Serializable]
    public struct Metadata
    {
        [field: SerializeField] public string key { get; private set; }
        [field: SerializeField] public string value { get; private set; }

        public Metadata(string k = "", string v = "")
        {
            key = "";
            value = "";
        }
    }

    #region Fields and Properties
    public static List<Selectable> ActiveSelectables { get; } = new List<Selectable>();

    public static Action SelectionChanged;
    public static Selectable SelectedSelectable { get; private set; }
    public static bool IsInElevationPhotoMode { get; private set; }
    public static UnityEvent ActiveSelectablesInSceneChanged { get; } = new();

    public EventHandler MouseOverStateChanged;
    public UnityEvent SelectableDestroyed { get; } = new();
    public UnityEvent ScaleUpdated { get; } = new();
    public UnityEvent Deselected { get; } = new();
    public Selectable ParentSelectable { get; private set; }

    public bool IsMouseOver { get; private set; }
    public bool IsDestroyed { get; private set; }

    public Dictionary<GizmoType, Dictionary<Axis, GizmoSetting>> GizmoSettings { get; } = new();
    public Vector3 OriginalLocalPosition { get; set; }
    public Vector3 OriginalLocalRotation { get; private set; }
    public string guid { get; set; }
    [field: SerializeField] public string GUID { get; private set; }
    [field: SerializeField] public AttachmentPoint ParentAttachmentPoint { get; set; }
    [field: SerializeField] public Sprite Thumbnail { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string SubPartName { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] private List<RoomBoundaryType> WallRestrictions { get; set; } = new();
    [field: SerializeField] public List<SelectableType> Types { get; private set; } = new();
    [field: SerializeField] private Vector3 InitialLocalPositionOffset { get; set; }
    [field: SerializeField] public bool isDestructible { get; private set; } = true;
    [field: SerializeField] public bool AllowInverseControl { get; private set; } = false;
    [field: SerializeField] private List<GizmoSetting> GizmoSettingsList { get; set; } = new();
    [field: SerializeField] public List<ScaleLevel> ScaleLevels { get; private set; } = new();
    [field: SerializeField] public bool useLossyScale { get; private set; }
    [field: SerializeField] private bool ZAlwaysFacesGround { get; set; }
    [field: SerializeField] private bool ZAlwaysFacesGroundElevationOnly { get; set; }
    [field: SerializeField] private bool ZAlignUpIsParentForward { get; set; }
    [field: SerializeField] public List<Measurable> Measurables { get; private set; }
    [field: SerializeField] private bool AlignForElevationPhoto { get; set; }
    [field: SerializeField] private bool ChangeHeightForElevationPhoto { get; set; }

    private List<Selectable> _assemblySelectables = new();
    private Dictionary<Selectable, Quaternion> _originalRotations = new();
    private Dictionary<Measurable, bool> _measurableActiveStates = new();
    private List<Vector3> _childScales = new();
    private Quaternion _originalRotation;
    private Transform _virtualParent;
    private HighlightEffect _highlightEffect;
    private GizmoHandler _gizmoHandler;
    private Quaternion _originalRotation2;
    private Camera _cameraRenderTextureElevation;
    public static Camera ActiveCameraRenderTextureElevation { get; private set; }

    /// <summary>
    /// If true, this is probably a ceiling mount
    /// </summary>
    private bool IsAssemblyRoot => Types.Contains(SelectableType.Mount);
    public bool IsArmAssembly => transform.root.TryGetComponent(out Selectable rootSelectable) && rootSelectable.IsAssemblyRoot;
    public bool IsSelected => SelectedSelectable == this;

    [field: SerializeField, ReadOnly] public ScaleLevel CurrentScaleLevel { get; private set; }
    [field: SerializeField, ReadOnly] public ScaleLevel CurrentPreviewScaleLevel { get; private set; }
    [field: HideInInspector] public UnityEvent<ScaleLevel> OnScaleChange;

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

    private bool CheckConstraints(float currentVal, float originalVal, float maxVal, float minVal, out float excess)
    {
        float diff = currentVal - originalVal;
        excess = diff > maxVal ? diff - maxVal : diff < minVal ? diff - minVal : 0f;
        return excess != 0;
    }

    public bool ExeedsMaxTranslation(out Vector3 totalExcess)
    {
        Vector3 adjustedTransform = transform.localRotation * transform.localPosition;

        float ignoreScale = GetGizmoSettingTranslateIgnoreBool() ? 1 : transform.localScale.z;

        float maxTranslationX = GetGizmoSettingMaxValue(GizmoType.Move, Axis.X) * transform.localScale.x;
        float maxTranslationY = GetGizmoSettingMaxValue(GizmoType.Move, Axis.Y) * transform.localScale.y;
        float maxTranslationZ = GetGizmoSettingMaxValue(GizmoType.Move, Axis.Z) * ignoreScale;

        float minTranslationX = GetGizmoSettingMinValue(GizmoType.Move, Axis.X) * transform.localScale.x;
        float minTranslationY = GetGizmoSettingMinValue(GizmoType.Move, Axis.Y) * transform.localScale.y;
        float minTranslationZ = GetGizmoSettingMinValue(GizmoType.Move, Axis.Z) * ignoreScale;

        Vector3 adjustedMaxTranslation = new Vector3(maxTranslationX, maxTranslationY, maxTranslationZ);
        Vector3 adjustedMinTranslation = new Vector3(minTranslationX, minTranslationY, minTranslationZ);
        totalExcess = default;
        bool exceedsX = IsGizmoSettingAllowed(GizmoType.Move, Axis.X)
                        && CheckConstraints(adjustedTransform.x,
                                            OriginalLocalPosition.x,
                                            adjustedMaxTranslation.x,
                                            adjustedMinTranslation.x,
                        out totalExcess.x);
        bool exceedsY = IsGizmoSettingAllowed(GizmoType.Move, Axis.Y) 
                        && CheckConstraints(adjustedTransform.y, 
                                            OriginalLocalPosition.y, 
                                            adjustedMaxTranslation.y, 
                                            adjustedMinTranslation.y, 
                        out totalExcess.y);
        bool exceedsZ = IsGizmoSettingAllowed(GizmoType.Move, Axis.Z) 
                        && CheckConstraints(adjustedTransform.z, 
                                            OriginalLocalPosition.z, 
                                            adjustedMaxTranslation.z, 
                                            adjustedMinTranslation.z, 
                        out totalExcess.z);
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

    public bool TryGetArmAssemblyRoot(out GameObject rootObj)
    {
        rootObj = null;
        if (transform.root.TryGetComponent<Selectable>(out var rootSelectable))
        {
            rootObj = rootSelectable.gameObject;
            return rootSelectable.IsAssemblyRoot;
        }
        return false;
    }

    public void SetScaleLevel(ScaleLevel scaleLevel, bool setSelected)
    {
        CurrentPreviewScaleLevel = scaleLevel;
        OnScaleChange.Invoke(CurrentPreviewScaleLevel);

        Quaternion storedRotation = transform.rotation;
        transform.rotation = _originalRotation;

        for (int j = 0; j < 2; j++) //not sure if still need to do this twice
        {
            Vector3 parentOriginalScale = transform.localScale;
            Vector3 newScale = new Vector3(transform.localScale.x, transform.localScale.y, scaleLevel.ScaleZ);
            transform.localScale = newScale;

            if (scaleLevel == CurrentScaleLevel)
            {
                //Debug.Log("Using stored child scales");
                for (int i = 0; i < transform.childCount; i++)
                    transform.GetChild(i).transform.localScale = _childScales[i];
            }
            else
            {
                // Debug.Log("Calculating child scales");
                Vector3 newParentScale = newScale;
                // Get the relative difference to the original scale
                var diffX = newParentScale.x / parentOriginalScale.x;
                var diffY = newParentScale.y / parentOriginalScale.y;
                var diffZ = newParentScale.z / parentOriginalScale.z;

                // Debug.Log($"Relative Difference ({diffX}, {diffY}, {diffZ})");

                // This inverts the scale differences
                var diffVector = new Vector3(1 / diffX, 1 / diffY, 1 / diffZ);

                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);

                    if (child.TryGetComponent(out IgnoreInverseScaling ignore)) continue;

                    Vector3 localDiff = child.transform.InverseTransformVector(diffVector);
                    // Debug.Log($"{child.name} Current Scale is ({child.transform.localScale.x}, {child.transform.localScale.y}, {child.transform.localScale.z})");
                    // Debug.Log($"Local Diff after InverseTransformVector for {child.name} is ({localDiff.x}, {localDiff.y}, {localDiff.z})");
                    if (child.TryGetComponent(out Selectable selectable))
                    {
                        if (selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z))
                        {
                            child.transform.localScale = Vector3.Scale(child.transform.localScale, diffVector);
                        }
                    }
                    else if (gameObject.TryGetComponent(out BoomHeadScaleHandler headScale))
                    {
                        child.transform.localScale = Vector3.Scale(child.transform.localScale, diffVector);
                    }
                    else
                    {
                        float x = Mathf.Abs(child.transform.localScale.x * localDiff.x);
                        float y = Mathf.Abs(child.transform.localScale.y * localDiff.y);
                        float z = Mathf.Abs(child.transform.localScale.z * localDiff.z);
                        // Debug.Log($"Applying new scale of ({x}, {y}, {z})");
                        child.transform.localScale = new Vector3(x, y, z);
                    }
                }
            }
        }

        if (setSelected)
        {
            ScaleLevels.ForEach((item) => item.Selected = false);
            scaleLevel.Selected = true;
            CurrentScaleLevel = scaleLevel;
            OnScaleChange.Invoke(CurrentScaleLevel);

            if (TryGetComponent(out ScaleGroup group))
            {
                ScaleGroupManager.OnScaleLevelChanged?.Invoke(group.id, CurrentScaleLevel);
            }

            StoreChildScales();
        }

        transform.rotation = storedRotation;
    }

    private void UpdateZScaling(bool setSelected)
    {
        if (ScaleLevels.Count == 0) return;
        //get closest scale in list
        ScaleLevel closest = ScaleLevels.OrderBy(item => Math.Abs(_gizmoHandler.CurrentScaleDrag.z - item.ScaleZ)).First();
        if (closest == CurrentPreviewScaleLevel && !setSelected) return;
        SetScaleLevel(closest, setSelected);
        ScaleUpdated?.Invoke();
    }

    private bool IsHittingCeiling()
    {
        RoomBoundary ceiling = RoomBoundary.GetRoomBoundary(RoomBoundaryType.Ceiling);

        var bounds = ceiling.MeshRenderer.bounds;
        if (!TryGetComponent<MeshFilter>(out var meshFilter)) return false;

        var verts = meshFilter.sharedMesh.vertices;

        foreach (var vert in verts)
        {
            if (bounds.Contains(transform.TransformPoint(vert))) return true;
        }

        return false;
    }

    public void StoreChildScales()
    {
        _childScales.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            _childScales.Add(child.transform.localScale);
            //Debug.Log($"Storing child scales");
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

    public float GetGizmoSettingMinValue(GizmoType gizmoType, Axis axis)
    {
        if (TryGetGizmoSetting(gizmoType, axis, out GizmoSetting gizmoSetting))
        {
            return gizmoSetting.GetMinValue;
        }
        else return 0;
    }

    public bool GetGizmoSettingTranslateIgnoreBool()
    {
        if(TryGetGizmoSetting(GizmoType.Move, Axis.Z, out GizmoSetting gizmoSetting))
        {
            return gizmoSetting.IgnoreScale;
        }
        else return false;
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

    private Bounds GetAssemblyBounds()
    {
        if (!IsAssemblyRoot)
        {
            if (TryGetArmAssemblyRoot(out GameObject rootObj))
            {
                return rootObj.GetComponent<Selectable>().GetAssemblyBounds();
            }
            else
            {
                throw new Exception("Could not get assembly root");
            }
        }

        List<MeshRenderer> renderers = GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToList();

        if (renderers.Count == 0)
        {
            throw new Exception("Arm assembly has no renderers!");
        }

        Bounds bounds = renderers[0].bounds;
        renderers.ForEach(renderer =>
        {
            bounds.Encapsulate(renderer.bounds);
        });

        return bounds;
    }

    public void SetAssemblyToDefaultRotations()
    {
        if (TryGetArmAssemblyRoot(out GameObject rootObj))
        {
            if (rootObj == gameObject)
            {
                _assemblySelectables = GetComponentsInChildren<Selectable>().ToList();
                //_assemblySelectables.Add(this);
                _originalRotations.Clear();
                Array.ForEach(_assemblySelectables.OrderBy(x => x.GetParentCount()).ToArray(), item =>
                {
                    if (item.AlignForElevationPhoto || item.ChangeHeightForElevationPhoto || item.ZAlwaysFacesGroundElevationOnly)
                    {
                        _originalRotations[item] = item.transform.localRotation;
                        item.transform.localRotation = item._originalRotation2;
                    }
                });
            }
            else
            {
                rootObj.GetComponent<Selectable>().SetAssemblyToDefaultRotations();
                return;
            }
        }
    }

    private void ToggleMeasurableActiveStates(bool active)
    {
        if (active)
        {
            _assemblySelectables.ForEach(item =>
            {
                if (item.Measurables.Count > 0)
                {
                    item.Measurables.ForEach(measurable =>
                    {
                        measurable.ArmAssemblyActiveInElevationPhotoMode = true;
                        _measurableActiveStates[measurable] = measurable.IsActive;
                        measurable.SetActive(true);
                    });
                }
            });
        }
        else
        {
            _measurableActiveStates.Keys.ToList().ForEach(item =>
            {
                item.ArmAssemblyActiveInElevationPhotoMode = false;
                item.SetActive(_measurableActiveStates[item]);
                float _ = 0;
                item.UpdateMeasurements(ref _);
            });
        }
    }

    private List<PdfExporter.PdfImageData> GetAssemblyPDFImageData(Camera camera)
    {
        var imageDatas = new List<PdfExporter.PdfImageData>();
        for (int i = 0; i < 2; i++)
        {
            foreach (Selectable selectable in _assemblySelectables.Where(x => x.ChangeHeightForElevationPhoto))
            {
                var newAngles = selectable.transform.localEulerAngles;
                var gizmoSetting = selectable.GizmoSettings[GizmoType.Rotate][Axis.Y];

                if (i == 0)
                {
                    newAngles.y = gizmoSetting.Invert ? gizmoSetting.GetMaxValue : gizmoSetting.GetMinValue;
                    selectable.transform.localEulerAngles = newAngles;
                    var childList = selectable.GetComponentsInChildren<Selectable>().ToList();
                    while (childList.FirstOrDefault(x => x.IsHittingCeiling()) != default)
                    {
                        float abs = Mathf.Abs(newAngles.y) - 1f;
                        if (abs < 0f) break;
                        newAngles.y = abs * Mathf.Sign(newAngles.y);
                        selectable.transform.localEulerAngles = newAngles;
                    }
                }
                else
                {
                    newAngles.y = gizmoSetting.Invert ? gizmoSetting.GetMinValue : gizmoSetting.GetMaxValue;
                }

                selectable.transform.localEulerAngles = newAngles;
            }

            _assemblySelectables.Where(x => x.ZAlwaysFacesGround || x.ZAlwaysFacesGroundElevationOnly).ToList().ForEach(item =>
            {
                item.FaceZTowardGround();
            });

            var bounds = GetAssemblyBounds();

            //take the photo
            imageDatas.Add(new PdfExporter.PdfImageData()
            {
                Path = GetElevationPhoto(camera, bounds, out var imageWidth, out var imageHeight, i),
                Width = imageWidth,
                Height = imageHeight
            });
        }

        return imageDatas;
    }

    public void ExportElevationPdf()
    {
        if (TryGetArmAssemblyRoot(out GameObject rootObj))
        {
            if (rootObj != gameObject)
            {
                rootObj.GetComponent<Selectable>().ExportElevationPdf();
                return;
            }

            // this obj is the ceiling mount
            IsInElevationPhotoMode = true;
            var camera = GetComponentInChildren<Camera>();
            ActiveCameraRenderTextureElevation = camera;

            SetAssemblyToDefaultRotations();
            _measurableActiveStates.Clear();
            ToggleMeasurableActiveStates(true);

            //store visibility states of all selectables in scene for later
            List<bool> visibilities = ActiveSelectables.ConvertAll(x => x.gameObject.activeSelf);

            //shut off all selectables in the scene except for the ones in this arm assembly
            ActiveSelectables.Where(x => !_assemblySelectables.Contains(x)).ToList().ForEach(x => x.gameObject.SetActive(false));

            PdfExporter.ExportElevationPdf(GetAssemblyPDFImageData(camera), _assemblySelectables);

            for (int i = 0; i < ActiveSelectables.Count; i++) ActiveSelectables[i].gameObject.SetActive(visibilities[i]);

            RestoreArmAssemblyRotations();
            _assemblySelectables.ForEach(x => x.FaceZTowardGround());
            IsInElevationPhotoMode = false;
            ToggleMeasurableActiveStates(false);
        }
    }

    public void RestoreArmAssemblyRotations()
    {
        if (TryGetArmAssemblyRoot(out GameObject rootObj))
        {
            if (rootObj == gameObject)
            {
                _assemblySelectables.ForEach(item =>
                {
                    if (item.AlignForElevationPhoto || item.ChangeHeightForElevationPhoto || item.ZAlwaysFacesGroundElevationOnly)
                        item.transform.localRotation = _originalRotations[item];
                });
            }
            else
            {
                rootObj.GetComponent<Selectable>().RestoreArmAssemblyRotations();
                return;
            }
        }
    }

    private string GetElevationPhoto(Camera camera, Bounds bounds, out int imageWidth, out int imageHeight, int fileIndex)
    {
        camera.enabled = true;
        camera.orthographic = true;
        Vector3 cameraOriginalPos = camera.transform.position;
        Vector3 outwardDirection = cameraOriginalPos - transform.position;
        camera.transform.position = bounds.center + (outwardDirection.normalized * bounds.extents.magnitude);
        camera.transform.LookAt(bounds.center, Vector3.up);
        camera.orthographicSize = bounds.extents.y;

        float addedHeight = 0.1f;
        _assemblySelectables.ForEach(item =>
        {
            if (item.Measurables.Count > 0)
            {
                item.Measurables.ForEach(measurable =>
                {
                    var validMeasurements = measurable.Measurements.Where(measurement => measurement.Measurable.ShowInElevationPhoto).ToList();
                    if (validMeasurements.Count > 0)
                    {
                        measurable.SetActive(true);
                        measurable.UpdateMeasurements(ref addedHeight, camera);
                        validMeasurements.ForEach(measurement =>
                        {
                            measurement.Measurer.MeasurementText.UpdateVisibilityAndPosition(camera, force: true);
                            measurement.Measurer.UpdateTransform(camera);
                            bounds.Encapsulate(measurement.Measurer.Renderer.bounds);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.up * 0.3f);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.down * 0.3f);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.right * 0.3f);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.left * 0.3f);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.forward * 0.3f);
                            bounds.Encapsulate(measurement.Measurer.TextPosition + Vector3.back * 0.3f);
                        });
                    }
                });
            }
        });
        //bounds.Encapsulate(new Vector3(bounds.center.x, -0.1f, bounds.center.z));
        camera.transform.position = bounds.center + (outwardDirection.normalized * bounds.extents.magnitude);
        camera.transform.LookAt(bounds.center, Vector3.up);
        camera.orthographicSize = bounds.extents.y;

        int safetyCounter = 1000;
        Vector2 screenPointMin = camera.WorldToScreenPoint(bounds.min);
        Vector2 screenPointMax = camera.WorldToScreenPoint(bounds.max);
        RenderTexture renderTexture = camera.targetTexture;

        bool IsPointInShot(Vector2 screenPoint)
        {
            return screenPoint.x > 0 && screenPoint.y > 0 && screenPoint.x < renderTexture.width && screenPoint.y < renderTexture.height;
        }

        while (--safetyCounter > 0)
        {
            camera.orthographicSize += 1f;
            screenPointMax = camera.WorldToScreenPoint(bounds.max);
            screenPointMin = camera.WorldToScreenPoint(bounds.min);
            if (IsPointInShot(screenPointMin) && IsPointInShot(screenPointMax))
            {
                break;
            }
        }

        if (safetyCounter == 0)
        {
            throw new Exception("Could not get bounds of Arm Assembly for photo");
        }

        imageWidth = Mathf.CeilToInt(Mathf.Abs(screenPointMax.x - screenPointMin.x));
        imageHeight = Mathf.CeilToInt(Mathf.Abs(screenPointMax.y - screenPointMin.y));

        Canvas.ForceUpdateCanvases();

        InGameLight.ToggleLights(false);
        Light cameraLight = camera.GetComponentInChildren<Light>(true);
        cameraLight.gameObject.SetActive(true);
        camera.Render();
        camera.enabled = false;
        camera.transform.position = cameraOriginalPos;
        cameraLight.gameObject.SetActive(false);
        InGameLight.ToggleLights(true);

        RenderTexture.active = renderTexture;

        Texture2D tex = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        float screenMinX = Mathf.Min(screenPointMin.x, screenPointMax.x);
        float screenMinY = Mathf.Min(screenPointMin.y, screenPointMax.y);

        tex.ReadPixels(new Rect(screenMinX, screenMinY, imageWidth, imageHeight), 0, 0);
        RenderTexture.active = null;

        byte[] pngData = tex.EncodeToPNG();

        string filenameImage1 = Application.persistentDataPath + $"/ExportedArmAssemblyElevationShot{fileIndex}.png";
        System.IO.File.WriteAllBytes(filenameImage1, pngData);
        return filenameImage1;
    }

    #region Monobehaviour
    private void Awake()
    {
        guid = Guid.NewGuid().ToString();
        if (!ConfigurationManager._instance.isDebug && GUID != "" && !ConfigurationManager._instance.isRoomBoundary(GUID)) gameObject.name = guid.ToString();

        ActiveSelectables.Add(this);
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
                ParentSelectable = selectable;
                break;
            }

            parent = parent.parent;
        }

        _cameraRenderTextureElevation = GetComponentInChildren<Camera>();
        if (_cameraRenderTextureElevation != null)
        {
            _cameraRenderTextureElevation.enabled = false;
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

        ActiveSelectablesInSceneChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (IsDestroyed) return;

        IsDestroyed = true;

        ActiveSelectables.Remove(this);

        if (SelectedSelectable == this)
        {
            Deselect();
        }

        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
        if (ParentAttachmentPoint != null)
        {
            ParentAttachmentPoint.DetachSelectable(this);
        }

        if (ParentSelectable != null)
        {
            Destroy(ParentSelectable.gameObject);
        }

        SelectableDestroyed?.Invoke();
        ActiveSelectablesInSceneChanged?.Invoke();
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
        if (ScaleLevels.Count > 0)
        {
            CurrentScaleLevel = ScaleLevels.First(item => item.ModelDefault);
            CurrentPreviewScaleLevel = CurrentScaleLevel;
            CurrentScaleLevel.ScaleZ = transform.localScale.z;

            StoreChildScales();

            ScaleLevels.ForEach(item =>
            {
                if (!item.ModelDefault)
                {
                    float perc = item.Size / CurrentScaleLevel.Size;
                    item.ScaleZ = CurrentScaleLevel.ScaleZ * perc;
                }
            });

            var defaultSelected = ScaleLevels.First(item => item.Selected);
            SetScaleLevel(defaultSelected, true);

            if (IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z))
            {
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

        _originalRotation2 = transform.localRotation;
        OriginalLocalPosition = transform.localPosition;
        //OriginalLocalRotation = transform.localEulerAngles;
        Vector3 adjustedOffsetVector = new Vector3(InitialLocalPositionOffset.x * transform.localScale.x, InitialLocalPositionOffset.y * transform.localScale.y, InitialLocalPositionOffset.z * transform.localScale.z);
        transform.localPosition += adjustedOffsetVector;
    }

    private void Update()
    {
        UpdateRaycastPlacementMode();
        FaceZTowardGround();
    }
    #endregion

    private void FaceZTowardGround()
    {
        if (ZAlwaysFacesGround || (ZAlwaysFacesGroundElevationOnly && IsInElevationPhotoMode))
        {
            float oldX = transform.localEulerAngles.x;
            transform.LookAt(transform.position + Vector3.down, ZAlignUpIsParentForward ? transform.parent.forward : transform.parent.right);
            //if (!IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z))
            //{
            transform.localEulerAngles = new Vector3(oldX, transform.localEulerAngles.y, 0);
            //}
        }
    }

    public async void StartRaycastPlacementMode()
    {
        if (ParentAttachmentPoint != null) return;
        DeselectAll();
        _highlightEffect.highlighted = true;

        await Task.Yield();
        if (!Application.isPlaying) return;

        _isRaycastPlacementMode = true;
    }

    public static float RoundToNearestHalfInch(float value)
    {
        float halfInch = 0.0127f;
        float modulo = value % halfInch;
        if (modulo <= halfInch / 2)
        {
            value -= modulo;
        }
        else
        {
            value += halfInch - modulo;
        }

        return value;
    }

    private async void UpdateRaycastPlacementMode()
    {
        if (!_isRaycastPlacementMode || _hasBeenPlaced) return;

        if (WallRestrictions[0] == RoomBoundaryType.Ceiling && OperatingRoomCamera.LiveCamera.CameraType == OperatingRoomCameraType.OrthoCeiling)
        {
            RoomBoundary.GetRoomBoundary(RoomBoundaryType.Ceiling).Collider.enabled = true;
        }

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = 1 << LayerMask.NameToLayer("Wall");
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, mask))
        {
            void SetPosition(RaycastHit hit)
            {
                Vector3 destination = hit.point;
                Vector3 normal = hit.normal;
                if (WallRestrictions[0] == RoomBoundaryType.Ceiling && OperatingRoomCamera.LiveCamera.CameraType == OperatingRoomCameraType.OrthoCeiling)
                {
                    destination += RoomBoundary.DefaultWallThickness * Vector3.down;
                    normal *= -1;
                }

                if (Types.Contains(SelectableType.Door))
                {
                    RoomBoundaryType roomBoundaryType = hit.collider.gameObject.GetComponent<RoomBoundary>().RoomBoundaryType;

                    float halfThickness = RoomBoundary.DefaultWallThickness / 2f;
                    if (roomBoundaryType == RoomBoundaryType.WallNorth)
                    {
                        destination.z = hit.collider.transform.position.z - halfThickness;
                        normal = -Vector3.forward;
                    }
                    else if (roomBoundaryType == RoomBoundaryType.WallSouth)
                    {
                        destination.z = hit.collider.transform.position.z + halfThickness;
                        normal = Vector3.forward;
                    }
                    else if (roomBoundaryType == RoomBoundaryType.WallWest)
                    {
                        destination.x = hit.collider.transform.position.x + halfThickness;
                        normal = Vector3.right;
                    }
                    else if (roomBoundaryType == RoomBoundaryType.WallEast)
                    {
                        destination.x = hit.collider.transform.position.x - halfThickness;
                        normal = -Vector3.right;
                    }


                    destination.y = 0;
                }

                if (UI_ToggleSnapping.SnappingEnabled)
                {
                    float yMag = Mathf.Abs(hit.normal.y);
                    float xMag = Mathf.Abs(hit.normal.x);
                    float zMag = Mathf.Abs(hit.normal.z);

                    if (yMag > xMag && yMag > zMag)
                    {
                        destination.x = RoundToNearestHalfInch(destination.x);
                        destination.z = RoundToNearestHalfInch(destination.z);
                    }
                    else if (xMag > yMag && xMag > zMag)
                    {
                        destination.y = RoundToNearestHalfInch(destination.y);
                        destination.z = RoundToNearestHalfInch(destination.z);
                    }
                    else if (zMag > xMag && zMag > yMag)
                    {
                        destination.y = RoundToNearestHalfInch(destination.y);
                        destination.x = RoundToNearestHalfInch(destination.x);
                    }
                }
                transform.SetPositionAndRotation(destination, Quaternion.LookRotation(normal));
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
            if (WallRestrictions[0] == RoomBoundaryType.Ceiling && OperatingRoomCamera.LiveCamera.CameraType == OperatingRoomCameraType.OrthoCeiling)
            {
                RoomBoundary.GetRoomBoundary(RoomBoundaryType.Ceiling).Collider.enabled = false;
            }
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
            if (!isDestructible) return;

            Deselect();

            Destroy(gameObject);
        }
    }

    private void Deselect(bool fireEvent = true)
    {
        if (!IsSelected) return;
        SelectedSelectable = null;
        _highlightEffect.highlighted = false;
        Deselected?.Invoke();

        if (fireEvent)
        {
            SelectionChanged?.Invoke();
        }
    }

    public void Select()
    {
        if (IsSelected || _isRaycastPlacementMode || GizmoHandler.GizmoBeingUsed) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect(false);
        }
        SelectedSelectable = this;
        _highlightEffect.highlighted = true;
        SelectionChanged?.Invoke();
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
    Wall,
    CeilingLight,
    Door,
    ServiceHeadPanel,
    ServiceHeadShelves,
    Tabletop
}