using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_MeasurementButton : MonoBehaviour
{
    private Toggle _toggle;
    private Measurable _currentMeasurable;
    public static UnityEvent Toggled = new();

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        Selectable.SelectionChanged += (o, e) =>
        {
            bool active = Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.Measurable != null;
            gameObject.SetActive(active);
            if (active)
            {
                _currentMeasurable = Selectable.SelectedSelectable.Measurable;
                _toggle.SetIsOnWithoutNotify(active && Selectable.SelectedSelectable.Measurable.IsActive);
            }
            else
            {
                _currentMeasurable = null;
            }
        };
        gameObject.SetActive(false);
    }

    public void OnToggle(bool isOn)
    {
        if (_currentMeasurable != null) 
        {
            _currentMeasurable.SetActive(isOn);
        }

        Toggled?.Invoke();
    }
}
