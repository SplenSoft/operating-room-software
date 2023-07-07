using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Measurer : MonoBehaviour
{
    private static List<Measurer> _measurers = new List<Measurer>();

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

    private static Measurer GetAvailableMeasurer()
    {
        var result = _measurers.FirstOrDefault(item => item.Measurement == null);
        if (result == default)
        {
            result = Instantiate(_measurers[0].gameObject).GetComponent<Measurer>();
            result.Measurement = null;
            _measurers.Add(result);
        }

        result.gameObject.SetActive(true);
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
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Vector3.Distance(Measurement.Origin, Measurement.HitPoint));
    }

    private void Awake()
    {
        _measurers.Add(this);
    }

    private void OnDestroy()
    {
        _measurers.Remove(this);
    }
}
