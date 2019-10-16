using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;


// This class sets up the bloom pass
public class PerObjectBloomPass : ScriptableRendererFeature
{
    public const int k_PerObjectBlurRenderLayerIndex = 5;

    private PerObjectBloomPassImpl m_perObjectPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_perObjectPass);
    }

    public override void Create()
    {
        m_perObjectPass = new PerObjectBloomPassImpl();
        m_perObjectPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
}


// This class implments the bloom effect
public class PerObjectBloomPassImpl : ScriptableRenderPass
{
    private const string k_PerObjectBloomTag = "Per Object Bloom";

    static readonly string kStencilWriteShaderName = "Hidden/Internal-StencilWrite";
    static readonly ShaderTagId kLightweightForwardShaderId = new ShaderTagId("LightweightForward");

    RenderTargetHandle m_PerObjectRenderTextureHandle;
    FilteringSettings m_PerObjectFilterSettings;
    Material m_BrightnessMaskMaterial;

    public PerObjectBloomPassImpl()
    {
        // Setup a target RT handle (it just wraps the int id)
        m_PerObjectRenderTextureHandle.Init(k_PerObjectBloomTag);

        m_PerObjectFilterSettings = new FilteringSettings(RenderQueueRange.opaque, -1, 1 << PerObjectBloomPass.k_PerObjectBlurRenderLayerIndex);

        // This just writes black values for anything that is rendered
        m_BrightnessMaskMaterial = CoreUtils.CreateEngineMaterial(kStencilWriteShaderName);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, cameraTextureDescriptor);

        ConfigureTarget(m_PerObjectRenderTextureHandle.Identifier());
        ConfigureClear(ClearFlag.All, Color.white);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_PerObjectBloomTag);

        using (new ProfilingSample(cmd, k_PerObjectBloomTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var camera = renderingData.cameraData.camera;

            // We want the same rendering result as the main opaque render
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

            // Setup render data from camera
            var drawSettings = CreateDrawingSettings(kLightweightForwardShaderId, ref renderingData, sortFlags);
            drawSettings.overrideMaterial = m_BrightnessMaskMaterial;
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_PerObjectFilterSettings);

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
