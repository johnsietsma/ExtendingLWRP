using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PerObjectBlurTarget : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.renderingLayerMask |= 1 << PerObjectBlurPass.k_PerObjectBlurRenderLayerIndex;
    }
}
