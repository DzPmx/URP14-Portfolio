using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RF01_ColotTintFeature : ScriptableRendererFeature
{
    public Shader shader;
    private Material material;
    [SerializeField] private Color colorTint;
    [SerializeField]private RenderPassEvent renderPassEvent;
    public class RF01_ColotTintPass : ScriptableRenderPass
    {
        private RTHandle sourceColor;
        private Material material;
        private Color color;
        private const string profilerTag = "ColorTint";
        private  ProfilingSampler colorTintSampler = new (profilerTag);
        private int postprocessingTexture = Shader.PropertyToID("_PostProcessTexture");
        private int colorTint = Shader.PropertyToID("_ColorTint");
        public RF01_ColotTintPass(Material material, Color color)
        {
            this.material = material;
            this.color = color;
        }
        public void SetUp(RTHandle sourceColor)
        {
            this.sourceColor = sourceColor;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get("后处理集成");
            material.SetTexture(postprocessingTexture,sourceColor);
            material.SetColor(colorTint,color);
            using (new ProfilingScope(buffer,colorTintSampler))
            {
                buffer.DrawProcedural(Matrix4x4.identity,material,0,MeshTopology.Triangles,3);
              
            }
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.Release();
        }

    }

    private RF01_ColotTintPass colotTintPass;
    
    /// <summary>
    /// Step 1 
    /// </summary>
    public override void Create()
    {
        this.name = "colorTint";
        material = CoreUtils.CreateEngineMaterial(shader);
        colotTintPass = new RF01_ColotTintPass(material,colorTint);
        colotTintPass.renderPassEvent = renderPassEvent;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType is CameraType.Game)
        {
            renderer.EnqueuePass(colotTintPass);
        }
        colotTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
         colotTintPass.SetUp(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
}