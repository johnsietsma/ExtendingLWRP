using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class PerObjectBlurPass : MonoBehaviour, IAfterOpaquePass
{
    public const int k_PerObjectBlurRenderLayerIndex = 5;

    [SerializeField]
    private Material m_BlurStencilMaterial;

    private PerObjectBlurPassImpl m_perObjectPass;

    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
    {
        if (m_perObjectPass == null) m_perObjectPass = new PerObjectBlurPassImpl(baseDescriptor, m_BlurStencilMaterial);
        return m_perObjectPass;
    }
}


public class PerObjectBlurPassImpl : ScriptableRenderPass
{

    private const string k_PerObjectBlurTag = "Per Object Blur";

    private Material m_BlurStencilMaterial;

    private RenderTextureDescriptor m_baseDescriptor;
    private RenderTargetHandle m_PerObjectRenderTextureHandle;
    private FilterRenderersSettings m_PerObjectFilterSettings;

    public PerObjectBlurPassImpl(RenderTextureDescriptor baseDescriptor, Material blurStencilMaterial)
    {
        RegisterShaderPassName("LightweightForward");

        m_baseDescriptor = baseDescriptor;
        m_BlurStencilMaterial = blurStencilMaterial;

        // Setup a target RT handle
        m_PerObjectRenderTextureHandle = new RenderTargetHandle();
        m_PerObjectRenderTextureHandle.Init(k_PerObjectBlurTag);

        m_PerObjectFilterSettings = new FilterRenderersSettings(true)
        {
            // Filter by any renderer tagged as per-object blur
            //renderingLayerMask = PerObjectBlurPass.k_PerObjectBlurRenderLayerIndex,
            renderQueueRange = RenderQueueRange.opaque,
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_PerObjectBlurTag);
        using (new ProfilingSample(cmd, k_PerObjectBlurTag))
        {

            cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, m_baseDescriptor);
            SetRenderTarget(
                cmd,
                m_PerObjectRenderTextureHandle.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare,
                ClearFlag.All,
                Color.black,
                m_baseDescriptor.dimension );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var camera = renderingData.cameraData.camera;

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None, renderingData.supportsDynamicBatching);
            drawSettings.SetOverrideMaterial(m_BlurStencilMaterial, 0);
            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_PerObjectFilterSettings);
        }


        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
        cmd.ReleaseTemporaryRT(m_PerObjectRenderTextureHandle.id);
    }
}
