using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ToggleLightSwitch : MonoBehaviour
{
    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();

        Selectable.SelectionChanged += (o, e) =>
        {
            bool b = Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.HasLights();
            gameObject.SetActive(b);

            if(b)
            {
                _toggle.isOn = Selectable.SelectedSelectable.gameObject.GetComponent<LightFactory>().isOn();
            }
        };

        gameObject.SetActive(false);

        _toggle.onValueChanged.AddListener((x) => {
            if(_toggle.isOn != Selectable.SelectedSelectable.gameObject.GetComponent<LightFactory>().isOn())
            {
                ToggleLights();
            }
        });
    }

    public void ToggleLights()
    {
        Selectable.SelectedSelectable.gameObject.GetComponent<LightFactory>().SwitchLight();
    }
}