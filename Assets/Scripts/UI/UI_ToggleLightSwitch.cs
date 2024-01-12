using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI Toggle Light Switch controls the logic for the UI Light Toggle
/// It only shows when a Light Factory containing Selectable has been selected
/// </summary>
public class UI_ToggleLightSwitch : MonoBehaviour
{
    Toggle _toggle; // internal reference to the toggle object
    LightFactory _selectedLight = null; // internal reference to the current selected light, to reduce getcomponent calls

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();

        Selectable.SelectionChanged += (o, e) =>
        {
            bool b = Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.GetComponent<LightFactory>(); // store result of the selectable & light checks for multiple uses
            gameObject.SetActive(b); // if there is a current selectable and it is a light, we display the UI

            if(b)
            {
                _selectedLight = Selectable.SelectedSelectable.gameObject.GetComponent<LightFactory>(); // store the value for reuse
                _toggle.isOn = _selectedLight.isOn(); // set the toggle to match the current state of the light (ON/OFF)
            }
        };

        gameObject.SetActive(false); // turn off the UI element on awake, as nothing is selected

        // lambda delegate for the toggle's value changing. 
        // This is done instead of the Editor to avoid an edge-case of the value applying to subsequently clicked lights
        _toggle.onValueChanged.AddListener((x) => {
            if(_toggle.isOn != _selectedLight.isOn())
            {
                _selectedLight.SwitchLight();
            }
        });
    }
}