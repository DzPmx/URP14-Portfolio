using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RF01_ColotTintFeature : ScriptableRendererFeature
{
    public Shader shader;
    private Material material;
    [SerializeField] private Color colorTint;
    [SerializeField] private RenderPassEvent renderPassEvent;
    
    private RF01_ColotTintPass colotTintPass;

    /// <summary>
    /// 第一步 创建初始化Pass
    /// </summary>
    public override void Create()
    {
        this.name = "colorTint";
        material = CoreUtils.CreateEngineMaterial(shader);
        colotTintPass = new RF01_ColotTintPass();
        colotTintPass.Create(material,colorTint);
        colotTintPass.renderPassEvent = renderPassEvent;
    }

    /// <summary>
    /// 添加汇入RenderPass的逻辑及Pass需要的输入
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType is CameraType.Reflection) return;
        renderer.EnqueuePass(colotTintPass);
        colotTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
    }
    /// <summary>
    /// 初始化Pass需要的RT
    /// </summary>
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType is CameraType.Reflection) return;
        colotTintPass.SetUp(renderer.cameraColorTargetHandle);
    }
    /// <summary>
    /// 销毁RF中和RenderPass中的资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(material);
        colotTintPass.Dispose();
    }
}
public class RF01_ColotTintPass : ScriptableRenderPass
{
    private RTHandle cameraColor;
    private RTHandle GrabTex;
    private Material material;
    private Color color;
    private const string profilerTag = "ColorTint";
    private ProfilingSampler colorTintSampler = new(profilerTag);
    private int colorTint = Shader.PropertyToID("_ColorTint");
        
    /// <summary>
    /// 传入Material和颜色参数
    /// </summary>
    public void Create(Material material,Color color)
    {
        this.material = material;
        this.color = color;
    }

    public void SetUp(RTHandle cameraColor)
    {
        this.cameraColor = cameraColor;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref GrabTex, descriptor,FilterMode.Bilinear,name:"_GrabTexture");
        cmd.SetGlobalTexture("_GrabTexture",GrabTex.nameID);
        ConfigureTarget(GrabTex);
        ConfigureClear(ClearFlag.All,Color.clear);
    }

    /// <summary>
    /// 渲染逻辑、每帧执行
    /// </summary>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer buffer = CommandBufferPool.Get("后处理集成");
        material.SetColor(colorTint, color);
        using (new ProfilingScope(buffer, colorTintSampler))
        {
            Blitter.BlitCameraTexture(buffer,cameraColor,GrabTex);
            CoreUtils.SetRenderTarget(buffer,cameraColor);
            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
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