using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

public class GrabPass : ScriptableRenderPass
{
    public GrabPass()
    {
        RegisterShaderPassName("GrabPass");
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {

    }
}
