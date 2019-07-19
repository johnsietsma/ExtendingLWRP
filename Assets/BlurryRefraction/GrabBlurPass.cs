using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

// Render features MUST be in a namespace to workaround crash in inspector UI
namespace CustomRenderPasses
{

    [CreateAssetMenu]
    public class GrabBlurPass : ScriptableRendererFeature
    {
        private GrabBlurPassImpl m_grabBlurPass;

        public override void Create()
        {
            m_grabBlurPass = new GrabBlurPassImpl(RenderPassEvent.AfterRenderingOpaques);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_grabBlurPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(m_grabBlurPass);
        }
    }


    public class GrabBlurPassImpl : ScriptableRenderPass
    {
        const string k_RenderGrabPassTag = "BlurredGrabPass";

        public Shader m_BlurShader;
        private CommandBufferBlur m_Blur;
        int m_BlurTemp1;
        int m_BlurTemp2;

        RenderTargetIdentifier sourceColor;

        public GrabBlurPassImpl(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            m_Blur = new CommandBufferBlur();

            m_BlurTemp1 = Shader.PropertyToID("_Temp1");
            m_BlurTemp2 = Shader.PropertyToID("_Temp2");
        }

        public void Setup(RenderTargetIdentifier sourceColor)
        {
            this.sourceColor = sourceColor;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);

            using (new ProfilingSample(cmd, k_RenderGrabPassTag))
            {
                // copy screen into temporary RT
                int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(screenCopyID, opaqueDesc, FilterMode.Bilinear);
                cmd.Blit(sourceColor, screenCopyID);

                // get two smaller RTs
                opaqueDesc.width /= 2;
                opaqueDesc.height /= 2;
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

}