using RTG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeasurementText : MonoBehaviour
{
    private static List<MeasurementText> _instances = new List<MeasurementText>();

    private static MeasurementText _master;

    [SerializeField, ReadOnly] private Measurer _measurer;
    private TextMeshProUGUI _text; 

    private void Awake()
    {
        if (_master == null)
        {
            Measurer.MeasurerAdded += (o, newMeasurer) =>
            {
                var newText = Instantiate(_master.gameObject, _master.transform.parent).GetComponent<MeasurementText>();
                newText._measurer = newMeasurer;
                newText.gameObject.SetActive(true);
                newText._measurer.ActiveStateToggled.AddListener(() =>
                {
                    newText.gameObject.SetActive(newText._measurer.gameObject.activeSelf);
                });
                newText._measurer.VisibilityToggled.AddListener(() =>
                {
                    newText._text.enabled = newText._measurer.IsRendererVisible;
                });
            };

            _master = this;
            gameObject.SetActive(false);
        }
        else
        {
            UI_MeasurementButton.Toggled.AddListener(() =>
            {
                if (_measurer != null)
                {
                    gameObject.SetActive(_measurer.gameObject.activeSelf);
                }
                
            });
            _text = GetComponent<TextMeshProUGUI>();
            _instances.Add(this);
        }
    }

    public void RotateTowardCamera(Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        Plane nearFrustrumPlane = planes[4];

        Vector3 planePoint = nearFrustrumPlane.ClosestPointOnPlane(transform.position);
        var vec = transform.position - planePoint;
        vec = vec.normalized;

        float angleOfMeasurer = Vector3.Angle(_measurer.transform.forward, Vector3.up);
        float angle2 = Vector3.Angle(_measurer.transform.forward, Vector3.down);

        var quat = Quaternion.LookRotation(vec);
        transform.SetPositionAndRotation(transform.position, quat);

        if (angleOfMeasurer < 10f || angle2 < 10f)
        {
            transform.Rotate(0, 0, 90);
        }
        
    }

    private void Update()
    {
        if (!_measurer.gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        _text.text = _measurer.Distance;
        transform.position = _measurer.TextPosition;
        RotateTowardCamera();
    }
}
