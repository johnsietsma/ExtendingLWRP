using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A small helper to set the correct render layer for bloom targets
[RequireComponent(typeof(Renderer))]
[ExecuteInEditMode()]
public class PerObjectBlurTarget : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.renderingLayerMask |= 1 << PerObjectBloomPass.k_PerObjectBlurRenderLayerIndex;
    }
}
