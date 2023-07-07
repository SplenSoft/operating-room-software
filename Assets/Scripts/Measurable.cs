using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Measurable : MonoBehaviour
{
    public class Measurement
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 HitPoint { get; set; }
        public Measurer Measurer { get; set; }
    }

    public static UnityEvent ActiveMeasurablesChanged { get; } = new UnityEvent();

    public static List<Measurable> ActiveMeasurables { get; } = new();
    public Dictionary<RoomBoundaryType, Measurement> Measurements { get; private set; } = new();

    public bool IsActive { get; private set; }

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
            Measurements.Values.ToList().ForEach(item =>
            {
                item.Measurer.Measurement = null;
                item.Measurer = null;
            });
        }
    }

    private void Update()
    {
        if (IsActive) 
        {
            RoomBoundary.Instances.ForEach(boundary =>
            {
                Vector3 raycastVector;
                switch (boundary.RoomBoundaryType)
                {
                    case RoomBoundaryType.WallNorth:
                        raycastVector = Vector3.forward;
                        break;
                    case RoomBoundaryType.WallSouth:
                        raycastVector = -Vector3.forward;
                        break;
                    case RoomBoundaryType.WallWest:
                        raycastVector = -Vector3.right;
                        break;
                    case RoomBoundaryType.WallEast:
                        raycastVector = Vector3.right;
                        break;
                    default:
                        return;
                }

                Ray ray = new Ray(transform.position, raycastVector);
                int layerMask = 1 << LayerMask.NameToLayer("Wall");

                if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000f, layerMask))
                {
                    Measurement measurement;
                    bool newMeasurement = false;
                    if (!Measurements.ContainsKey(boundary.RoomBoundaryType))
                    {
                        measurement = new Measurement();
                        Measurements.Add(boundary.RoomBoundaryType, measurement);
                        newMeasurement = true;
                    }
                    else
                    {
                        measurement = Measurements[boundary.RoomBoundaryType];
                    }

                    measurement.Origin = ray.origin;
                    measurement.HitPoint = raycastHit.point;

                    if (newMeasurement)
                    {
                        ActiveMeasurablesChanged?.Invoke();
                    }
                }
            });
        }
    }
}
