using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_MeasurementButton : MonoBehaviour
{
    private Toggle _toggle;
    private List<Measurable> _currentMeasurables = new();
    public static UnityEvent Toggled = new();

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        Selectable.SelectionChanged += (o, e) =>
        {
            if (!Application.isPlaying) return;
            if (gameObject == null) return;

            bool active = Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.Measurables.Count > 0;
            gameObject.SetActive(active);
            if (active)
            {
                _currentMeasurables = Selectable.SelectedSelectable.Measurables;
                _toggle.SetIsOnWithoutNotify(active && Selectable.SelectedSelectable.Measurables[0].IsActive);
            }
            else
            {
                _currentMeasurables = null;
            }
        };
        gameObject.SetActive(false);
    }

    public void OnToggle(bool isOn)
    {
        if (_currentMeasurables != null) 
        {
            _currentMeasurables.ForEach(item =>
            {
                item.SetActive(isOn);
            });
            
        }

        Toggled?.Invoke();
    }
}
