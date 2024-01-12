using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MaterialPalette : MonoBehaviour
{
    [field: SerializeField] public MaterialElement[] elements { get; private set; }
    [field: SerializeField] public MeshRenderer meshRenderer { get; private set; }

    void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    void Start()
    {
        for (int i = 0; i < elements.Count(); i++)
        {
            if (elements[i]._zeroStart)
            {
                Assign(elements[i].materials[0], i);
            }
        }
    }

    public void Assign(Material material, int i)
    {
        Material[] mats = meshRenderer.materials;
        mats[i] = material;
        meshRenderer.materials = mats;

        if (TryGetComponent<ScaleMaterialTextureWithTransform>(out ScaleMaterialTextureWithTransform s))
        {
            s.Scale();
        }
    }

    public void Assign(string material, int i)
    {
        Material[] mats = meshRenderer.materials;
        try
        {
            mats[i] = elements[i].materials.Single(x => x.name == material);
            meshRenderer.materials = mats;

            if (TryGetComponent<ScaleMaterialTextureWithTransform>(out ScaleMaterialTextureWithTransform s))
            {
                s.Scale();
            }
        }
        catch(InvalidOperationException exception)
        {
            Debug.LogError($"Failed to find material \"{material}\" within element array {i}");
        }

    }
}

[Serializable]
public class MaterialElement
{
    [field: SerializeField] public bool _zeroStart { get; private set; }
    [field: SerializeField] public Material[] materials;
}
