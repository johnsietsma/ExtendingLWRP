using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CustomRenderPasses;

// A small helper to set the correct render layer for bloom targets
[RequireComponent(typeof(Renderer))]
[ExecuteInEditMode()]
public class PerObjectBloomTarget : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.renderingLayerMask |= 1 << PerObjectBloomPass.k_PerObjectBlurRenderLayerIndex;
    }

    void OnEnable()
    {
        PerObjectBloomPassImpl.bloomMeshes.Add(GetComponent<MeshRenderer>());
    }

    void OnDisable()
    {
        PerObjectBloomPassImpl.bloomMeshes.Remove(GetComponent<MeshRenderer>());
    }
}
