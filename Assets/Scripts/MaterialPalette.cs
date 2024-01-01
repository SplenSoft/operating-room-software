using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialPalette : MonoBehaviour
{
    [field: SerializeField] public bool zeroStart { get; private set; }
    public Material[] materials;
    [field: SerializeField] public MeshRenderer meshRenderer {get; private set; }

    void Awake()
    {
        if(meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    void Start()
    {
        if(zeroStart) Assign(materials[0]);
    }

    public void Assign(Material material)
    {
        meshRenderer.material = material;
        if(TryGetComponent<ScaleMaterialTextureWithTransform>(out ScaleMaterialTextureWithTransform s))
        {
            s.Scale();
        }
    }
}
