using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Measurer : MonoBehaviour
{
    public static List<Measurer> Measurers = new List<Measurer>();
    public static EventHandler<Measurer> MeasurerAdded;
    public UnityEvent ActiveStateToggled = new();
    public UnityEvent VisibilityToggled = new();
    public bool IsRendererVisible => Renderer.enabled;
    public MeshRenderer Renderer { get; private set; }
    public static bool Initialized { get; private set; }
    public List<LineRenderer> LineRenderers { get; private set; } = new();
    
    public bool AllowInElevationPhotoMode => Measurement != null && Measurement.Measurable.ArmAssemblyActiveInElevationPhotoMode && Measurement.MeasurementType == MeasurementType.ToArmAssemblyOrigin;

    [RuntimeInitializeOnLoadMethod]
    private static void OnAppStart()
    {
        Measurable.ActiveMeasurablesChanged.AddListener(() =>
        {
            Measurable.ActiveMeasurables.ForEach(activeMeasurable =>
            {
                activeMeasurable.Measurements.ForEach(item => 
                { 
                    if (item.Measurer == null)
                    {
                        item.Measurer = GetAvailableMeasurer();
                        item.Measurer.Measurement = item;
                    }
                });
            });
        });
        Initialized = true;
    }

    public Measurable.Measurement Measurement { get; private set; }

    public string Distance { get; private set; } = string.Empty;

    private Transform _childTransform;
    public Vector3 TextPosition => _childTransform.position;

    private static Measurer GetAvailableMeasurer()
    {
        var result = Measurers.FirstOrDefault(item => item.Measurement == null);
        if (result == default)
        {
            result = Instantiate(Measurers[0].gameObject).GetComponent<Measurer>();
            result.Measurement = null;
            //_measurers.Add(result);
        }

        result.gameObject.SetActive(true);
        result.ActiveStateToggled?.Invoke();
        return result;
    }

    private void Update()
    {
        if (Measurement == null) 
        {
            Renderer.enabled = true;
            gameObject.SetActive(false);
            return;
        }

        if (IsRendererVisible) 
        {
            UpdateTransform();
        }

        UpdateVisibility();
    }

    public void SetMeasurement(Measurable.Measurement measurement)
    {
        Measurement = measurement;
        if (measurement != null) 
        {
            UpdateTransform();
            UpdateVisibility();
        }
    }

    public void UpdateTransform()
    {
        if (Measurement == null) return;
        transform.position = Measurement.Origin;
        transform.LookAt(Measurement.HitPoint);
        float distanceMeters = Vector3.Distance(Measurement.Origin, Measurement.HitPoint);
        float distanceFeet = Mathf.Floor(distanceMeters.ToFeet());
        float distanceInches = Mathf.Round((distanceMeters.ToFeet() - distanceFeet) * 12f * 10f) / 10f;
        Distance = $"{distanceFeet}' {distanceInches}\"";
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, distanceMeters);
    }

    public void UpdateVisibility(Camera camera = null)
    {
        bool isEnabled = true;
        if (Selectable.IsInElevationPhotoMode)
        {
            isEnabled = AllowInElevationPhotoMode;
        }
        else if (FreeLookCam.IsActive)
        {
            isEnabled = Measurement != null && Measurement.MeasurementType != MeasurementType.ToArmAssemblyOrigin;
        }
        
        if (isEnabled)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }
            Vector3 measurerForward = transform.forward;
            Vector3 cameraForward = camera.transform.forward;
            float angle = Vector3.Angle(cameraForward, measurerForward);
            isEnabled = angle > 10 && angle < 170;
        }
        
        if ((!Renderer.enabled && isEnabled) || (Renderer.enabled && !isEnabled))
        {
            Renderer.enabled = isEnabled;
            VisibilityToggled?.Invoke();
        }
    }

    private void Awake()
    {
        _childTransform = transform.GetChild(0);
        Renderer = GetComponentInChildren<MeshRenderer>();
        LineRenderers = GetComponentsInChildren<LineRenderer>(true).ToList();
        LineRenderers.ForEach(x => x.enabled = false);
    }

    private void Start()
    {
        Measurers.Add(this);
        MeasurerAdded?.Invoke(this, this);
    }

    private void OnDestroy()
    {
        Measurers.Remove(this);
    }
}
