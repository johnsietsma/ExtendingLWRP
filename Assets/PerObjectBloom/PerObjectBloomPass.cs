using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


// This class sets up the bloom pass
public class PerObjectBloomPass : MonoBehaviour, IAfterOpaquePass
{
    public const int k_PerObjectBlurRenderLayerIndex = 5;

    private PerObjectBloomPassImpl m_perObjectPass;

    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
    {
        if (m_perObjectPass == null) m_perObjectPass = new PerObjectBloomPassImpl(baseDescriptor);
        return m_perObjectPass;
    }
}


// This class implments the bloom effect
public class PerObjectBloomPassImpl : ScriptableRenderPass
{
    private const string k_PerObjectBloomTag = "Per Object Bloom";

    private Material m_brightnessMaskMaterial;

    private RenderTextureDescriptor m_baseDescriptor;
    private RenderTargetHandle m_PerObjectRenderTextureHandle;
    private FilterRenderersSettings m_PerObjectFilterSettings;

    public PerObjectBloomPassImpl(RenderTextureDescriptor baseDescriptor)
    {
        // All shaders with this lightmode will be in this pass
        RegisterShaderPassName("LightweightForward");

        m_baseDescriptor = baseDescriptor;

        // This just writes black values for anything that is rendered
        m_brightnessMaskMaterial = new Material(Shader.Find("Hidden/Internal-StencilWrite"));

        // Setup a target RT handle (it just wraps the int id)
        m_PerObjectRenderTextureHandle = new RenderTargetHandle();
        m_PerObjectRenderTextureHandle.Init(k_PerObjectBloomTag);

        m_PerObjectFilterSettings = new FilterRenderersSettings(true)
        {
            // Render all opaque objects
            renderQueueRange = RenderQueueRange.opaque,
            // Filter further by any renderer tagged as per-object blur
            renderingLayerMask = 1<<PerObjectBloomPass.k_PerObjectBlurRenderLayerIndex
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_PerObjectBloomTag);
        using (new ProfilingSample(cmd, k_PerObjectBloomTag))
        {

            cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, m_baseDescriptor);
            SetRenderTarget(
                cmd,
                m_PerObjectRenderTextureHandle.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare,
                ClearFlag.All,
                Color.white, // Clear to white, the stencil writes black values
                m_baseDescriptor.dimension // Create a buffer the same size as the color buffer
                );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var camera = renderingData.cameraData.camera;

            // We want the same rendering result as the main opaque render
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

            // Setup render data from camera
            var drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None, 
                renderingData.supportsDynamicBatching);

            // Everything gets drawn with the stencil shader
            drawSettings.SetOverrideMaterial(m_brightnessMaskMaterial, 0);

            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_PerObjectFilterSettings);

            // Set a global texture id so we can access this later on
            cmd.SetGlobalTexture("_PerObjectBloomMask", m_PerObjectRenderTextureHandle.id);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);

        // When rendering is done, clean up our temp RT
        cmd.ReleaseTemporaryRT(m_PerObjectRenderTextureHandle.id);
    }
}
