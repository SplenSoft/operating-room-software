using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Measurer : MonoBehaviour
{
    private static List<Measurer> _measurers = new List<Measurer>();
    public static EventHandler<Measurer> MeasurerAdded;
    public UnityEvent ActiveStateToggled = new();

    [RuntimeInitializeOnLoadMethod]
    private static void OnAppStart()
    {
        Measurable.ActiveMeasurablesChanged.AddListener(() =>
        {
            Measurable.ActiveMeasurables.ForEach(item =>
            {
                item.Measurements.Values.ToList().ForEach(item => 
                { 
                    if (item.Measurer == null)
                    {
                        item.Measurer = GetAvailableMeasurer();
                        item.Measurer.Measurement = item;
                    }
                });
            });
        });
    }

    public Measurable.Measurement Measurement { get; set; }

    public string Distance { get; private set; } = string.Empty;

    private Transform _childTransform;
    public Vector3 TextPosition => _childTransform.position;

    private static Measurer GetAvailableMeasurer()
    {
        var result = _measurers.FirstOrDefault(item => item.Measurement == null);
        if (result == default)
        {
            result = Instantiate(_measurers[0].gameObject).GetComponent<Measurer>();
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
            gameObject.SetActive(false);
            return;
        }

        transform.position = Measurement.Origin;
        transform.LookAt(Measurement.HitPoint);
        float distanceMeters = Vector3.Distance(Measurement.Origin, Measurement.HitPoint);
        float distanceFeet = Mathf.Floor(distanceMeters.ToFeet());
        float distanceInches = Mathf.Round((distanceMeters.ToFeet() - distanceFeet) / 12f * 10f) / 10f;
        Distance = $"{distanceFeet}' {distanceInches}\"";
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, distanceMeters);
    }

    private void Awake()
    {
        _childTransform = transform.GetChild(0);
    }

    private void Start()
    {
        _measurers.Add(this);
        MeasurerAdded?.Invoke(this, this);
    }

    private void OnDestroy()
    {
        _measurers.Remove(this);
    }
}
