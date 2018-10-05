using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class GrabPass : MonoBehaviour, IAfterSkyboxPass
{
    public Shader m_BlurShader;

    private GrabPassImpl m_grabPass;


    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
    {
        if (m_grabPass == null) m_grabPass = new GrabPassImpl(m_BlurShader, colorHandle);
        return m_grabPass;
    }
}


public class GrabPassImpl : ScriptableRenderPass
{
    const string k_RenderGrabPassTag = "GrabPass";

    public Shader m_BlurShader;
    private Material m_Material;
    private RenderTargetHandle m_ColorHandle;

    public GrabPassImpl(Shader blurShader, RenderTargetHandle colorHandle)
    {
        RegisterShaderPassName("GrabPass");
        m_BlurShader = blurShader;
        m_ColorHandle = colorHandle;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer buf = CommandBufferPool.Get(k_RenderGrabPassTag);

        using (new ProfilingSample(buf, k_RenderGrabPassTag))
        {
            if (!m_Material)
            {
                m_Material = new Material(m_BlurShader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
            }

            // copy screen into temporary RT
            int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
            RenderTextureDescriptor opaqueDesc = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            buf.GetTemporaryRT(screenCopyID, opaqueDesc, FilterMode.Bilinear);
            buf.Blit(m_ColorHandle.Identifier(), screenCopyID);

            opaqueDesc.width /= 2;
            opaqueDesc.height /= 2;

            // get two smaller RTs
            int blurredID = Shader.PropertyToID("_Temp1");
            int blurredID2 = Shader.PropertyToID("_Temp2");
            buf.GetTemporaryRT(blurredID, opaqueDesc, FilterMode.Bilinear);
            buf.GetTemporaryRT(blurredID2, opaqueDesc, FilterMode.Bilinear);

            // downsample screen copy into smaller RT, release screen RT
            buf.Blit(screenCopyID, blurredID);
            buf.ReleaseTemporaryRT(screenCopyID);

            // horizontal blur
            buf.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
            buf.Blit(blurredID, blurredID2, m_Material);
            // vertical blur
            buf.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
            buf.Blit(blurredID2, blurredID, m_Material);
            // horizontal blur
            buf.SetGlobalVector("offsets", new Vector4(4.0f / Screen.width, 0, 0, 0));
            buf.Blit(blurredID, blurredID2, m_Material);
            // vertical blur
            buf.SetGlobalVector("offsets", new Vector4(0, 4.0f / Screen.height, 0, 0));
            buf.Blit(blurredID2, blurredID, m_Material);

            buf.SetGlobalTexture("_GrabBlurTexture", blurredID);
        }

        context.ExecuteCommandBuffer(buf);
        CommandBufferPool.Release(buf);
    }
}
