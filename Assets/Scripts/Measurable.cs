using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class Measurable : MonoBehaviour
{
    public class Measurement
    {
        public Measurement(Measurable measurable) 
        {
            Measurable = measurable;
        }

        public bool IsValid { get; set; }
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 HitPoint { get; set; }
        public Measurer Measurer { get; set; }
        public Measurable Measurable { get; }
        public MeasurementType MeasurementType { get; set; }
        public RoomBoundaryType RoomBoundaryType { get; set; }
    }

    public static UnityEvent ActiveMeasurablesChanged { get; } = new UnityEvent();
    private static readonly float _lineRendererSizeScalar = 0.005f;
    public static List<Measurable> ActiveMeasurables { get; } = new();
    //public Dictionary<MeasurementType, Measurement> Measurements { get; private set; } = new();
    public List<Measurement> Measurements { get; } = new();
    [field: SerializeField] public List<MeasurementType> MeasurementTypes { get; private set; } = new();
    [field: SerializeField] private bool ForwardOnly { get; set; }
    private AttachmentPoint HighestAssemblyAttachmentPoint { get; set; }
    //[field: SerializeField] private List<LineRenderer> LineRenderers { get; set; } = new();
    public bool ArmAssemblyActiveInElevationPhotoMode { get; set; }

    public bool IsActive { get; private set; }

    private async void Awake()
    {
        MeasurementTypes = MeasurementTypes.Distinct().ToList();
        //LineRenderers = GetComponentsInChildren<LineRenderer>(true).ToList();
        while (!Measurer.Initialized)
        {
            await Task.Yield();
        }
        Initialize();
    }

    private void Initialize()
    {
        bool newMeasurement = false;
        MeasurementTypes.ForEach(item =>
        {
            if (item == MeasurementType.Walls)
            {
                if (ForwardOnly)
                {
                    Measurements.Add(new Measurement(this)
                    {
                        MeasurementType = item,
                    });
                }
                else
                {
                    new List<RoomBoundaryType>()
                    {
                        RoomBoundaryType.WallEast,
                        RoomBoundaryType.WallWest,
                        RoomBoundaryType.WallSouth,
                        RoomBoundaryType.WallNorth,
                    }.ForEach(type =>
                    {
                        Measurements.Add(new Measurement(this)
                        {
                            MeasurementType = item,
                            RoomBoundaryType = type
                        });
                    });
                }
                
            }
            else
            {
                Measurements.Add(new Measurement(this)
                {
                    MeasurementType = item
                });
            }
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

        //SetActive(true);
        //SetActive(false);
        Debug.Log($"Measurable {gameObject.name} initialized");
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
                //item.Measurer = null;
            });
        }
    }

    private void OnDestroy()
    {
        Measurements.ToList().ForEach(item =>
        {
            item.Measurer.SetMeasurement(null);
            item.Measurer.LineRenderers.ForEach(item => 
            {
                if (item != null)
                    item.enabled = false;
            });
            item.Measurer = null;
        });
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

    private static Dictionary<RoomBoundaryType, Vector3> _wallDirectionVectors = new ()
    {
        { RoomBoundaryType.WallNorth, Vector3.forward },
        { RoomBoundaryType.WallSouth, -Vector3.forward },
        { RoomBoundaryType.WallEast, Vector3.right },
        { RoomBoundaryType.WallWest, -Vector3.forward }
    };

    private void UpdateMeasurementViaRaycast(Vector3 direction, Measurement measurement)
    {
        Ray ray = new Ray(transform.position, direction);
        int mask = LayerMask.GetMask("Wall", "Selectable");
        measurement.IsValid = false;
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000f, mask))
        {
            var obj = raycastHit.collider.gameObject;
            if (obj.layer == LayerMask.NameToLayer("Wall") || obj.CompareTag("Wall"))
            {
                measurement.Origin = ray.origin;
                measurement.HitPoint = raycastHit.point;
                measurement.IsValid = true;
            }
        }
        //if (Physics.Raycast(ray, out raycastHit, 1000f, 1 << LayerMask.NameToLayer("Wall")))
        //{
        //    measurement.Origin = ray.origin;
        //    measurement.HitPoint = raycastHit.point;
        //}
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

    public void UpdateMeasurements(ref float heightMod, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        foreach (var item in Measurements)
        {
            switch (item.MeasurementType)
            {
                case MeasurementType.Walls:
                    if (ForwardOnly)
                    {
                        UpdateMeasurementViaRaycast(transform.forward, item);
                    }
                    else
                    {
                        UpdateMeasurementViaRaycast(_wallDirectionVectors[item.RoomBoundaryType], item);
                    }
                    break;
                case MeasurementType.Ceiling:
                    UpdateMeasurementViaRaycast(Vector3.up, item);
                    break;
                case MeasurementType.Floor:
                    UpdateMeasurementViaRaycast(Vector3.down, item);
                    break;
                case MeasurementType.ToArmAssemblyOrigin:
                    item.IsValid = true;
                    Vector3 addedHeight = Vector3.up * heightMod;
                    Vector3 origin = transform.position;
                    item.HitPoint = HighestAssemblyAttachmentPoint.transform.position + addedHeight;
                    origin.y = HighestAssemblyAttachmentPoint.transform.position.y;
                    item.Origin = origin + addedHeight;
                    
                    var measurer = item.Measurer;
                    if (item.Measurer != null)
                    {
                        if (Selectable.IsInElevationPhotoMode)
                        {
                            measurer.UpdateTransform();
                            measurer.UpdateVisibility(camera);
                        }
                        measurer.LineRenderers[0].enabled = measurer.IsRendererVisible;
                        measurer.LineRenderers[1].enabled = measurer.IsRendererVisible;
                        if (measurer.IsRendererVisible)
                        {
                            measurer.LineRenderers[0].positionCount = 2;
                            Vector3 line1Start = addedHeight + origin;
                            Vector3 line1End = transform.position;
                            measurer.LineRenderers[0].SetPosition(0, line1Start);
                            measurer.LineRenderers[0].SetPosition(1, line1End);
                            measurer.LineRenderers[0].startWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(line1Start, camera);
                            measurer.LineRenderers[0].endWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(line1End, camera);

                            measurer.LineRenderers[1].positionCount = 2;
                            Vector3 line2Start = addedHeight + HighestAssemblyAttachmentPoint.transform.position;
                            Vector3 line2End = HighestAssemblyAttachmentPoint.transform.position;
                            measurer.LineRenderers[1].SetPosition(0, line2Start);
                            measurer.LineRenderers[1].SetPosition(1, line2End);
                            measurer.LineRenderers[1].startWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(line2Start, camera);
                            measurer.LineRenderers[1].endWidth = _lineRendererSizeScalar * GetDistanceToCameraPlane(line2End, camera);
                            heightMod += heightMod;
                        }
                    }
                    break;
            }
        }
    }

    private void Update()
    {
        if (!IsActive) return;
        float _ = 0;
        UpdateMeasurements(ref _);
    }
}

public enum MeasurementType
{ 
    Walls,
    Floor,
    Ceiling,
    ToArmAssemblyOrigin
}
    