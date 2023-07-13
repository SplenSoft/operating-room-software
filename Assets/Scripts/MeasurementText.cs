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
                    newText._text.enabled = newText._measurer.IsRendererVisibile;
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

    private void Update()
    {
        if (!_measurer.gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        _text.text = _measurer.Distance;
        transform.position = _measurer.TextPosition;
    }
}
