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

    public class RF01_ColotTintPass : ScriptableRenderPass
    {
        private RTHandle sourceColor;
        private Material material;
        private Color color;
        private const string profilerTag = "ColorTint";
        private ProfilingSampler colorTintSampler = new(profilerTag);
        private int postprocessingTexture = Shader.PropertyToID("_PostProcessTexture");
        private int colorTint = Shader.PropertyToID("_ColorTint");

        /// <summary>
        /// 构造函数
        /// </summary>
        public RF01_ColotTintPass(Material material, Color color)
        {
            this.material = material;
            this.color = color;
        }

        public void SetUp(RTHandle sourceColor)
        {
            this.sourceColor = sourceColor;
        }

        /// <summary>
        /// 渲染逻辑、每帧执行
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get("后处理集成");
            material.SetTexture(postprocessingTexture, sourceColor);
            material.SetColor(colorTint, color);
            using (new ProfilingScope(buffer, colorTintSampler))
            {
                buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
            }

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.Release();
        }
    }

    private RF01_ColotTintPass colotTintPass;

    /// <summary>
    /// 第一步 创建初始化Pass
    /// </summary>
    public override void Create()
    {
        this.name = "colorTint";
        material = CoreUtils.CreateEngineMaterial(shader);
        colotTintPass = new RF01_ColotTintPass(material, colorTint);
        colotTintPass.renderPassEvent = renderPassEvent;
    }

    /// <summary>
    /// 添加汇入RenderPass的逻辑及Pass需要的输入
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType is CameraType.Game)
        {
            renderer.EnqueuePass(colotTintPass);
        }

        colotTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
    }
    /// <summary>
    /// 初始化Pass需要的RT
    /// </summary>
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        colotTintPass.SetUp(renderer.cameraColorTargetHandle);
    }
    /// <summary>
    /// 销毁RF中和RenderPass中的资源
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
}