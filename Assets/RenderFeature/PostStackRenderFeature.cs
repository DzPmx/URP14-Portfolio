using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Renderfeature.VolumeStack;
using RenderFeature.RenderPass;
public class PostStackRenderFeature : ScriptableRendererFeature
{
    private Shader shader;
    private Material material;

    private ColorTintRenderPass colotTintPass;

    /// <summary>
    /// 第一步 创建初始化Pass
    /// </summary>
    public override void Create()
    {
        this.name = "colorTint";
        shader=Shader.Find("MyURPShader/ShaderURPPostProcessing");
        material = CoreUtils.CreateEngineMaterial(shader);
        colotTintPass = new ColorTintRenderPass();
        colotTintPass.Create(material);
        colotTintPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
     
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
