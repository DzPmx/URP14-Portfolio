using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Renderfeature.VolumeStack;

public class ColorTintRenderPass : ScriptableRenderPass
{
    private RTHandle cameraColor;
    private RTHandle GrabTex;
    private Material material;
    private ColorTint colorTint;
    private const string profilerTag = "ColorTint";
    private ProfilingSampler colorTintSampler = new(profilerTag);
    private int colorTintID = Shader.PropertyToID("_ColorTint");

    /// <summary>
    /// 传入Material和颜色参数
    /// </summary>
    public void Create(Material material, ColorTint colorTint)
    {
        this.material = material;
        this.colorTint = colorTint;
    }

    public void SetUp(RTHandle cameraColor)
    {
        this.cameraColor = cameraColor;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref GrabTex, descriptor, FilterMode.Bilinear, name: "_GrabTexture");
        cmd.SetGlobalTexture("_GrabTexture", GrabTex.nameID);
        ConfigureTarget(GrabTex);
        ConfigureClear(ClearFlag.Color, Color.clear);
    }


    /// <summary>
    /// 渲染逻辑、每帧执行
    /// </summary>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer buffer = CommandBufferPool.Get("后处理集成");
        material.SetColor(colorTintID, colorTint.color.value);
        using (new ProfilingScope(buffer, colorTintSampler))
        {
            Blitter.BlitCameraTexture(buffer, cameraColor, GrabTex);
            CoreUtils.SetRenderTarget(buffer, cameraColor);
            buffer.DrawProcedural(Matrix4x4.identity, material, (int)PostStackPass.ColorTint, MeshTopology.Triangles,
                3);
        }

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        buffer.Dispose();
    }

    public void Dispose()
    {
        GrabTex?.Release();
    }
}