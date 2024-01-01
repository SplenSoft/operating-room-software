using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialPalette : MonoBehaviour
{
    [field: SerializeField] public bool zeroStart { get; private set; }
    public Material[] materials;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if(zeroStart) meshRenderer.material = materials[0];
        GetComponent<ScaleMaterialTextureWithTransform>().Scale();
    }

    public void Assign(Material material)
    {
        meshRenderer.material = material;
        GetComponent<ScaleMaterialTextureWithTransform>().Scale();
    }
}
