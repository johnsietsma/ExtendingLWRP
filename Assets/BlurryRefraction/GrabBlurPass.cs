using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class GrabBlurPass : MonoBehaviour, IAfterSkyboxPass
{
    private GrabBlurPassImpl m_grabBlurPass;

    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
    {
        if (m_grabBlurPass == null) m_grabBlurPass = new GrabBlurPassImpl(colorHandle);
        return m_grabBlurPass;
    }
}


public class GrabBlurPassImpl : ScriptableRenderPass
{
    const string k_RenderGrabPassTag = "BlurredGrabPass";

    public Shader m_BlurShader;
    private RenderTargetHandle m_ColorHandle;
    private CommandBufferBlur m_Blur;
    int m_BlurTemp1;
    int m_BlurTemp2;

    public GrabBlurPassImpl(RenderTargetHandle colorHandle)
    {
        m_ColorHandle = colorHandle;
        m_Blur = new CommandBufferBlur();

        m_BlurTemp1 = Shader.PropertyToID("_Temp1");
        m_BlurTemp2 = Shader.PropertyToID("_Temp2");
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);

        using (new ProfilingSample(cmd, k_RenderGrabPassTag))
        {
            // copy screen into temporary RT
            int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
            RenderTextureDescriptor opaqueDesc = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            cmd.GetTemporaryRT(screenCopyID, opaqueDesc, FilterMode.Bilinear);
            cmd.Blit(m_ColorHandle.Identifier(), screenCopyID);

            // get two smaller RTs
            cmd.GetTemporaryRT(m_BlurTemp1, opaqueDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(m_BlurTemp2, opaqueDesc, FilterMode.Bilinear);

            // downsample screen copy into smaller RT, release screen RT
            cmd.Blit(screenCopyID, m_BlurTemp1);
            cmd.ReleaseTemporaryRT(screenCopyID);

            opaqueDesc.width /= 2;
            opaqueDesc.height /= 2;

            // Setup blur commands
            m_Blur.SetupCommandBuffer(cmd, m_BlurTemp1, m_BlurTemp2);

            // Set texture id so we can use it later
            cmd.SetGlobalTexture("_GrabBlurTexture", m_BlurTemp1);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException("cmd");

        cmd.ReleaseTemporaryRT(m_BlurTemp1);
        cmd.ReleaseTemporaryRT(m_BlurTemp2);
    }
}
