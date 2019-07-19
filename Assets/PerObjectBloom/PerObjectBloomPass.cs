using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

// Render features MUST be in a namespace to workaround crash in inspector UI
namespace CustomRenderPasses
{

    // This class sets up the bloom pass
    public class PerObjectBloomPass : ScriptableRendererFeature
    {
        public const int k_PerObjectBlurRenderLayerIndex = 5;

        private PerObjectBloomPassImpl m_perObjectPass;

        public override void Create()
        {
            m_perObjectPass = new PerObjectBloomPassImpl(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_perObjectPass.Setup(renderingData.cameraData.cameraTargetDescriptor);
            renderer.EnqueuePass(m_perObjectPass);
        }
    }


    // This class implments the bloom effect
    public class PerObjectBloomPassImpl : ScriptableRenderPass
    {
        private const string k_PerObjectBloomTag = "Per Object Bloom";
        private const string k_PerObjectBloomTag2 = "Per Object Bloom2";
        static readonly ShaderTagId k_DepthOnlyShaderTagId = new ShaderTagId("LightweightForward");

        private Material m_brightnessMaskMaterial;

        private RenderTextureDescriptor m_baseDescriptor;
        private RenderTargetHandle m_PerObjectRenderTextureHandle;
        private FilteringSettings m_PerObjectFilterSettings;

        public PerObjectBloomPassImpl(RenderPassEvent evt)
        {
            renderPassEvent = evt;

            // This just writes black values for anything that is rendered
            m_brightnessMaskMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-StencilWrite");

            // Setup a target RT handle (it just wraps the int id)
            m_PerObjectRenderTextureHandle = new RenderTargetHandle();
            m_PerObjectRenderTextureHandle.Init(k_PerObjectBloomTag);

            m_PerObjectFilterSettings = new FilteringSettings()
            {
                // Render all opaque objects
                renderQueueRange = RenderQueueRange.all,
                // Filter further by any renderer tagged as per-object blur
                renderingLayerMask = uint.MaxValue// 1 << PerObjectBloomPass.k_PerObjectBlurRenderLayerIndex
            };
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_baseDescriptor = baseDescriptor;
        }

        public static List<MeshRenderer> bloomMeshes = new List<MeshRenderer>();

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_PerObjectBloomTag);
            using (new ProfilingSample(cmd, k_PerObjectBloomTag))
            {
                cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, m_baseDescriptor);
                CoreUtils.SetRenderTarget(
                    cmd,
                    m_PerObjectRenderTextureHandle.Identifier(),
                    ClearFlag.All,
                    Color.white // Clear to white, the stencil writes black values
                    );

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var camera = renderingData.cameraData.camera;

                // We want the same rendering result as the main opaque render
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

                // Setup render data from camera
                var drawSettings = CreateDrawingSettings(k_DepthOnlyShaderTagId, ref renderingData, SortingCriteria.OptimizeStateChanges);

                // Everything gets drawn with the stencil shader
                drawSettings.overrideMaterial = m_brightnessMaskMaterial;
                drawSettings.overrideMaterialPassIndex = 0;

                //context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_PerObjectFilterSettings);
                foreach (var mesh in bloomMeshes)
                    cmd.DrawMesh(mesh.GetComponent<MeshFilter>().sharedMesh, mesh.transform.localToWorldMatrix, m_brightnessMaskMaterial, 0, 0);

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
}