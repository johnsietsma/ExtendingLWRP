using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;

[Serializable]
[PostProcess(typeof(PerObjectBloomRenderer), PostProcessEvent.AfterStack, "Custom/PerObjectBloom")]
public sealed class PerObjectBloom : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Bloom Threshold")]
    public FloatParameter bloomThreshold = new FloatParameter { value = 0.5f };

    [Range(0f, 5f), Tooltip("Bloom Amount")]
    public FloatParameter bloomAmount = new FloatParameter { value = 1.1f };
}


public sealed class PerObjectBloomRenderer : PostProcessEffectRenderer<PerObjectBloom>
{
    CommandBufferBlur m_Blur;
    int m_BlurTemp1;
    int m_BlurTemp2;
    Material m_MaskedBrightnessBlit;
    Material m_AdditiveBlit;

    const string k_MaskedBightnessBlitShader = "Hidden/MaskedBrightnessBlit";
    const string k_AdditiveBlitShader = "Hidden/AdditiveBlit";

    public PerObjectBloomRenderer()
    {
        m_Blur = new CommandBufferBlur();

        m_BlurTemp1 = Shader.PropertyToID("_Temp1");
        m_BlurTemp2 = Shader.PropertyToID("_Temp2");

        m_MaskedBrightnessBlit = CoreUtils.CreateEngineMaterial(k_MaskedBightnessBlitShader);
        m_AdditiveBlit = CoreUtils.CreateEngineMaterial(k_AdditiveBlitShader);
    }

    public override void Render(PostProcessRenderContext context)
    {
        CommandBuffer cmd = context.command;

        // Create our temp working buffers, work at quarter size
        context.GetScreenSpaceTemporaryRT(cmd, m_BlurTemp1, 0, context.sourceFormat,
        RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width / 2, context.height / 2);
        context.GetScreenSpaceTemporaryRT(cmd, m_BlurTemp2, 0, context.sourceFormat,
        RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width / 2, context.height / 2);

        // Copy all values about our brightness and inside our mask to a temp buffer
        m_MaskedBrightnessBlit.SetFloat("_BloomThreshold", settings.bloomThreshold);
        cmd.Blit(context.source, m_BlurTemp1, m_MaskedBrightnessBlit);

        // Setup command for blurring the buffer
        m_Blur.SetupCommandBuffer(cmd, m_BlurTemp1, m_BlurTemp2);

        // Blit the blurred brightness back into the color buffer, optionally increasing the brightness
        m_AdditiveBlit.SetFloat("_AdditiveAmount", settings.bloomAmount);
        cmd.Blit(m_BlurTemp1, context.source, m_AdditiveBlit);

        // Blit to the destination buffer
        cmd.BlitFullscreenTriangle(context.source, context.destination);

        // Cleanup
        cmd.ReleaseTemporaryRT(m_BlurTemp1);
        cmd.ReleaseTemporaryRT(m_BlurTemp2);
    }
}