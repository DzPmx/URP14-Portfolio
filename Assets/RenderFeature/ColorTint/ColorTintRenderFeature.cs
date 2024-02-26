using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Renderfeature.VolumeStack;

public class ColorTintRenderFeature : ScriptableRendererFeature
{
    private Shader shader;
    private Material material;
    private ColorTint colorTint;
    private ColorTintRenderPass colotTintPass;


    public override void Create()
    {
        colorTint = GetVolume();

        this.name = "ColorTint";
        shader = Shader.Find("MyURPShader/ShaderURPPostProcessing");
        material = CoreUtils.CreateEngineMaterial(shader);
        colotTintPass = new ColorTintRenderPass();
        colotTintPass.Create(material, colorTint);
        colotTintPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }


    public ColorTint GetVolume()
    {
        var stack = VolumeManager.instance.stack;
        ColorTint colorTint = stack.GetComponent<ColorTint>();
        return colorTint;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        colotTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
        if (renderingData.cameraData.cameraType is CameraType.Reflection) return;
        renderer.EnqueuePass(colotTintPass);
       
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType is CameraType.Reflection) return;
        colotTintPass.SetUp(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(material);
        colotTintPass.Dispose();
    }
}