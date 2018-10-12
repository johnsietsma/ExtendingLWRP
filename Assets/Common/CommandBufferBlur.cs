using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Set up a CommandBuffer to do a blur
public class CommandBufferBlur
{
    private Material m_Material;

    public CommandBufferBlur()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/SeparableGlassBlur");
        m_Material.hideFlags = HideFlags.HideAndDontSave;
    }

    public void SetupCommandBuffer( CommandBuffer cmd, int blurTemp1, int blurTemp2 )
    {
        // horizontal blur
        cmd.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
        cmd.Blit(blurTemp1, blurTemp2, m_Material);
        // vertical blur
        cmd.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
        cmd.Blit(blurTemp2, blurTemp1, m_Material);
        // horizontal blur
        cmd.SetGlobalVector("offsets", new Vector4(4.0f / Screen.width, 0, 0, 0));
        cmd.Blit(blurTemp1, blurTemp2, m_Material);
        // vertical blur
        cmd.SetGlobalVector("offsets", new Vector4(0, 4.0f / Screen.height, 0, 0));
        cmd.Blit(blurTemp2, blurTemp1, m_Material);
    }
}
