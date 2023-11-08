using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightFactory : MonoBehaviour
{
    [SerializeField] bool on = false;
    [Header("Light Builder")]
    [Tooltip("The point where the light will be position on Start")]
    [SerializeField] Transform lightAttachPoint;
    Light light;

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
        if(on)
        {
            emissiveMaterial.EnableKeyword("_EMISSION");
            light.enabled = true;
        }
        else
        {
            emissiveMaterial.DisableKeyword("_EMISSION");
            light.enabled = false;
        }
    }

    void BuildLights()
    {
        light = lightAttachPoint.AddComponent<Light>();

        light.type = LightType.Spot;
        light.enabled = false;
    }
}
