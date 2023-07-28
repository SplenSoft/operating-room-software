using RTG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeasurementText : MonoBehaviour
{
    public static List<MeasurementText> Instances { get; } = new List<MeasurementText>();

    private static MeasurementText _master;

    [SerializeField, ReadOnly] private Measurer _measurer;
    public TextMeshProUGUI Text; 

    private void Awake()
    {
        if (_master == null)
        {
            Measurer.MeasurerAdded += (o, newMeasurer) =>
            {
                var newText = Instantiate(_master.gameObject, _master.transform.parent).GetComponent<MeasurementText>();
                newText._measurer = newMeasurer;
                newText._measurer.MeasurementText = newText;
                newText.gameObject.SetActive(true);
                newText._measurer.ActiveStateToggled.AddListener(() =>
                {
                    newText.gameObject.SetActive(newText._measurer.gameObject.activeSelf);
                });
                newText._measurer.VisibilityToggled.AddListener(() =>
                {
                    newText.Text.enabled = newText._measurer.IsRendererVisible;
                });
            };

            _master = this;
            gameObject.SetActive(false);
        }
        else
        {
            UI_MeasurementButton.Toggled.AddListener(CheckActiveState);

            Measurable.ActiveMeasurablesChanged.AddListener(CheckActiveState);

            Text = GetComponent<TextMeshProUGUI>();
            Instances.Add(this);
        }
    }

    public void CheckActiveState()
    {
        if (_measurer != null)
        {
            gameObject.SetActive(_measurer.IsRendererVisible);
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

    public void Update()
    {
        UpdateVisibilityAndPosition();
    }

    public void UpdateVisibilityAndPosition(Camera camera = null)
    {
        if (!_measurer.gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        if (camera == null)
        {
            camera = Camera.main;
        }

        Text.text = _measurer.Distance;
        transform.position = _measurer.TextPosition;
        RotateTowardCamera(camera);
    }
}
