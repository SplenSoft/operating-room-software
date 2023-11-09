using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class LightFactory : MonoBehaviour
{
    [SerializeField] bool on = false;
    [Header("Light Builder")]
    [Tooltip("The point where the light will be position on Start")]
    [SerializeField] Transform lightAttachPoint;
    [SerializeField] [Range(1500, 20000)] float tempature = 3000;
    [SerializeField] [Range(1, 50)] float intensity = 1;
    [SerializeField] float innerAngle = 15;
    [SerializeField] float outerAngle = 20;
    Light _light;

    [Header("Emission Settings")]
    [SerializeField] GameObject emissiveObject;
    Material emissiveMaterial;
    [SerializeField] Color emissionColor;

    void Start()
    {
        emissiveMaterial = emissiveObject.GetComponent<Renderer>().material;
        emissiveMaterial.SetColor("_EmissionColor", emissionColor);

        BuildLights();
    }

    public void SwitchLight()
    {
        on = !on;
        ToggleEmissive();
        _light.enabled = on;
    }

    public bool isOn()
    {
        return on;
    }

    void BuildLights()
    {
        _light = lightAttachPoint.AddComponent<Light>();

        _light.type = LightType.Spot;
        _light.useColorTemperature = true;
        _light.colorTemperature = tempature;
        _light.intensity = intensity;
        _light.innerSpotAngle = innerAngle;
        _light.spotAngle = outerAngle;
        _light.enabled = on;
    }

    void ToggleEmissive()
    {
        if(on)
        {
            emissiveMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            emissiveMaterial.DisableKeyword("_EMISSION");
        }
    }
}
