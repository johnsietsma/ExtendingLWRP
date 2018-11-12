using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class DistortionPass : MonoBehaviour, IAfterTransparentPass
{
    private DistortionPassImpl m_distortionPass;

    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
    {
        if (m_distortionPass == null) m_distortionPass = new DistortionPassImpl(baseDescriptor, colorHandle);
        return m_distortionPass;
    }
}


public class DistortionPassImpl : ScriptableRenderPass
{
    const string k_DistortionPassTag = "DistortionPass";

    private RenderTargetHandle m_ColorHandle;
    private Material m_DistortionApplyMaterial;
    private RenderTextureDescriptor m_DistortionVectorDescriptor;
    private FilterRenderersSettings m_DistortionFilterSettings;
    private RenderTargetHandle m_DistortionTextureHandle; // TODO local to render func?

    public DistortionPassImpl(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
    {
        //RegisterShaderPassName("LightweightForward");
        RegisterShaderPassName("DistortionVectors");

        m_ColorHandle = colorHandle;

        m_DistortionTextureHandle = new RenderTargetHandle();
        m_DistortionTextureHandle.Init(k_DistortionPassTag);

        m_DistortionApplyMaterial = CoreUtils.CreateEngineMaterial("NotHidden/DistortionApply");

        m_DistortionVectorDescriptor = new RenderTextureDescriptor(
            baseDescriptor.width, 
            baseDescriptor.height, 
            RenderTextureFormat.ARGBHalf);

        m_DistortionVectorDescriptor.useMipMap = false;

        m_DistortionFilterSettings = new FilterRenderersSettings(true)
        {
            renderQueueRange = RenderQueueRange.opaque
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_DistortionPassTag);

        using (new ProfilingSample(cmd, k_DistortionPassTag))
        {
            cmd.GetTemporaryRT(m_DistortionTextureHandle.id, m_DistortionVectorDescriptor);

            SetRenderTarget(
                cmd,
                m_DistortionTextureHandle.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare,
                ClearFlag.All,
                Color.black,
                m_DistortionVectorDescriptor.dimension // Create a buffer the same size as the color buffer
                );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var camera = renderingData.cameraData.camera;

            // We want the same rendering result as the main opaque render
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

            // Setup render data from camera
            DrawRendererSettings drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None,
                renderingData.supportsDynamicBatching);

            // Draw the distortion buffer
            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_DistortionFilterSettings);

            // Distortion effect
            // Set a global texture id so we can access this later on
            cmd.SetGlobalTexture("_DistortionTex", m_DistortionTextureHandle.id);
            cmd.Blit(m_ColorHandle.id, m_ColorHandle.id, m_DistortionApplyMaterial, 0);

            cmd.ReleaseTemporaryRT(m_DistortionTextureHandle.id);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
