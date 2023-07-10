using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleMaterialTextureWithTransform : MonoBehaviour
{
    private MeshRenderer _meshRenderer;
    [SerializeField] private bool _useX;
    [SerializeField] private bool _useY;
    [SerializeField] private bool _useZ;
    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        RoomSize.RoomSizeChanged += (o, e) =>
        {
            bool factor1Set = false;
            bool factor2Set = false;
            float factor1 = 1f;
            float factor2 = 1f;
            if (_useX)
            {
                factor1 = transform.localScale.x;
                factor1Set = true;
            }

            if (_useY) 
            {
                if (!factor1Set)
                {
                    factor1 = transform.localScale.y;
                    factor1Set = true;
                }
                else
                {
                    factor2 = transform.localScale.y;
                    factor2Set = true;
                }
            }

            if (_useZ) 
            { 
                if (!factor2Set)
                {
                    factor2 = transform.localScale.y;
                }
            }

            _meshRenderer.material.SetTextureScale("_MainTex", new Vector2(factor1, factor2));
        };
    }
}
