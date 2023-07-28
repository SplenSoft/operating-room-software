using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using static Measurable;

public class Measurable : MonoBehaviour
{
    public class Measurement
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 HitPoint { get; set; }
        public Measurer Measurer { get; set; }
        public MeasurementType MeasurementType { get; set; }
    }

    public static UnityEvent ActiveMeasurablesChanged { get; } = new UnityEvent();
    private static readonly float _lineRendererSizeScalar = 0.01f;
    public static List<Measurable> ActiveMeasurables { get; } = new();
    //public Dictionary<MeasurementType, Measurement> Measurements { get; private set; } = new();
    public List<Measurement> Measurements { get; } = new();
    [field: SerializeField] public List<MeasurementType> MeasurementTypes { get; private set; } = new();
    [field: SerializeField] private bool ForwardOnly { get; set; }
    private AttachmentPoint HighestAssemblyAttachmentPoint { get; set; }
    //[field: SerializeField] private List<LineRenderer> LineRenderers { get; set; } = new();

    public bool IsActive { get; private set; }

    private void Awake()
    {
        MeasurementTypes = MeasurementTypes.Distinct().ToList();
        //LineRenderers = GetComponentsInChildren<LineRenderer>(true).ToList();
        
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Measurer.Initialized);

        bool newMeasurement = false;
        MeasurementTypes.ForEach(item =>
        {
            Measurements.Add(new Measurement() 
            {
                MeasurementType = item
            });
            newMeasurement = true;
        });

        if (newMeasurement)
        {
            ActiveMeasurablesChanged?.Invoke();
        }

        if (MeasurementTypes.Contains(MeasurementType.ToArmAssemblyOrigin))
        {
            Transform parent = transform.parent;
            AttachmentPoint attachmentPoint = null;
            while (parent != null)
            {
                //Debug.Log($"{parent.gameObject.name}");
                if (parent.gameObject.TryGetComponent<AttachmentPoint>(out var point))
                {
                    attachmentPoint = point;
                }

                parent = parent.parent;
            }

            if (attachmentPoint == null)
            {
                throw new Exception("Could not find attachment point for arm origin measurable");
            }

            HighestAssemblyAttachmentPoint = attachmentPoint;
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;

        if (IsActive && !ActiveMeasurables.Contains(this))
        {
            ActiveMeasurables.Add(this);
            ActiveMeasurablesChanged?.Invoke();
        }
        else if (!IsActive && ActiveMeasurables.Contains(this))
        {
            ActiveMeasurables.Remove(this);
            ActiveMeasurablesChanged?.Invoke();
            Measurements.ToList().ForEach(item =>
            {
                item.Measurer.SetMeasurement(null);
                item.Measurer.LineRenderers.ForEach(item => item.enabled = false);
                item.Measurer = null;
            });
        }
    }

    public void SetForElevationPhoto()
    {
        
    }

    private int GetTotalNeededMeasurements()
    {
        int count = 0;
        MeasurementTypes.ForEach(item =>
        {
            switch (item)
            {
                case MeasurementType.Walls:
                    count += 4;
                    break;
                case MeasurementType.Floor:
                case MeasurementType.Ceiling:
                case MeasurementType.ToArmAssemblyOrigin:
                    count++;
                    break;
            }
        });
        return count;
    }

    private static List<Vector3> _wallDirectionVectors = new List<Vector3>
    {
        Vector3.forward,
        -Vector3.forward,
        -Vector3.right,
        Vector3.right
    };

    private void UpdateMeasurementViaRaycast(Vector3 direction, ref int measurementIndex)
    {
        Ray ray = new Ray(transform.position, direction);
        int layerMask = 1 << LayerMask.NameToLayer("Wall");

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000f, layerMask))
        {
            Measurements[measurementIndex].Origin = ray.origin;
            Measurements[measurementIndex].HitPoint = raycastHit.point;
        }
        measurementIndex++;
    } 

    private float GetCeilingYValue()
    {
        return RoomBoundary.Instances.Where(x => x.RoomBoundaryType == RoomBoundaryType.Ceiling).ToList()[0].transform.position.y;
    }

    private float GetDistanceToCameraPlane(Vector3 point, Camera camera = null)
    {
        if (camera == null) 
        {
            camera = Camera.main;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        Plane nearFrustrumPlane = planes[4];

        Vector3 planePoint = nearFrustrumPlane.ClosestPointOnPlane(point);
        return Vector3.Distance(point, planePoint);
    }

    public void UpdateMeasurements()
    {
        int measurementIndex = 0;
        MeasurementTypes.ForEach(item =>
        {
            switch (item)
            {
                case MeasurementType.Walls:
                    _wallDirectionVectors.ForEach(vector => UpdateMeasurementViaRaycast(vector, ref measurementIndex));
                    break;
                case MeasurementType.Ceiling:
                    UpdateMeasurementViaRaycast(Vector3.up, ref measurementIndex);
                    break;
                case MeasurementType.Floor:
                    UpdateMeasurementViaRaycast(Vector3.down, ref measurementIndex);
                    break;
                case MeasurementType.ToArmAssemblyOrigin:
                    Vector3 origin = transform.position;
                    Measurements[measurementIndex].HitPoint = HighestAssemblyAttachmentPoint.transform.position;
                    origin.y = HighestAssemblyAttachmentPoint.transform.position.y;
                    Measurements[measurementIndex].Origin = origin;
                    var measurer = Measurements[measurementIndex].Measurer;
                    if (Measurements[measurementIndex].Measurer != null)
                    {
                        measurer.LineRenderers[0].enabled = true;
                        measurer.LineRenderers[0].positionCount = 2;
                        measurer.LineRenderers[0].SetPosition(0, origin);
                        measurer.LineRenderers[0].SetPosition(1, transform.position);
                        measurer.LineRenderers[0].startWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(origin);
                        measurer.LineRenderers[0].endWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(transform.position);

                        measurer.LineRenderers[1].enabled = true;
                        measurer.LineRenderers[1].positionCount = 2;
                        measurer.LineRenderers[1].SetPosition(0, HighestAssemblyAttachmentPoint.transform.position);
                        measurer.LineRenderers[1].SetPosition(1, HighestAssemblyAttachmentPoint.transform.position);
                        measurer.LineRenderers[1].startWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(HighestAssemblyAttachmentPoint.transform.position);
                        measurer.LineRenderers[1].endWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(HighestAssemblyAttachmentPoint.transform.position);
                    }

                    measurementIndex++;
                    break;
            }
        });
    }

    private void Update()
    {
        if (!IsActive) return;

        UpdateMeasurements();
    }
}

public enum MeasurementType
{ 
    Walls,
    Floor,
    Ceiling,
    ToArmAssemblyOrigin
}
    