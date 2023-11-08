using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ToggleLightSwitch : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += (o, e) =>
        {
            gameObject.SetActive(Selectable.SelectedSelectable != null && Selectable.SelectedSelectable.HasLights());
        };

        gameObject.SetActive(false);
    }

    public void ToggleLights()
    {
        Selectable.SelectedSelectable.gameObject.GetComponent<LightFactory>().SwitchLight();
    }
}