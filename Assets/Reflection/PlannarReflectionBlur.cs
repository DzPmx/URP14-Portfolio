using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlannarReflectionBlur : ScriptableRendererFeature
{
    PlannerReflectionBlurRenderPass blurRenderPass;

    public override void Create()
    {
        name = "PlannarReflectionBlur";
        blurRenderPass = new PlannerReflectionBlurRenderPass();
        blurRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.camera.CompareTag("Reflection"))
        {
            return;
        }
        blurRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(blurRenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        blurRenderPass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        blurRenderPass.Dispose();
    }
}
public class PlannerReflectionBlurRenderPass : ScriptableRenderPass
{
    private Material material;
    private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Blur";
    private int dualBlurParamsID = Shader.PropertyToID("_DualBlurOffset");
    private RTHandle source;
    private RTHandle tempRT1;
    private RTHandle tempRT2;
    private PlannarReflectionBlurVolume volume;
    public void Setup(RTHandle cameraColor)
    {
        source = cameraColor;
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (material==null)
        {
            material=CoreUtils.CreateEngineMaterial(shaderName);
        }
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack;
        volume = stack.GetComponent<PlannarReflectionBlurVolume>();
        if (!volume.IsActive())
        {
            return;
        }
        CommandBuffer cmd = CommandBufferPool.Get("PlannarReflectionBlur");
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        descriptor.useMipMap = true;
        RenderingUtils.ReAllocateIfNeeded(ref tempRT1, in descriptor, FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_DualBlurTex1");
        using (new ProfilingScope(cmd, new ProfilingSampler("PlannarReflectionBlur")))
        {
            Blitter.BlitCameraTexture(cmd, source, tempRT1, bilinear: true);
            material.SetFloat(dualBlurParamsID, volume.blurRadius.value);
            int scaleTimes = 0;

            for (int i = 0; i < volume.blurTimes.value; i++)
            {
                if (descriptor.height < 130)
                {
                    break;
                }

                descriptor.height /= 2;
                descriptor.width /= 2;
                RenderingUtils.ReAllocateIfNeeded(ref tempRT2, in descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_DualBlurTex2");
                scaleTimes++;
                Blitter.BlitCameraTexture(cmd, tempRT1, tempRT2, material, (int)BlurPass.DualBlurDown);
                CoreUtils.Swap(ref tempRT1, ref tempRT2);
            }

            for (int i = 0; i < scaleTimes - 1; i++)
            {
                descriptor.height *= 2;
                descriptor.width *= 2;
                RenderingUtils.ReAllocateIfNeeded(ref tempRT2, in descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_DualBlurTex2");
                Blitter.BlitCameraTexture(cmd, tempRT1, tempRT2, material, (int)BlurPass.DualBlurUp);
                CoreUtils.Swap(ref tempRT1, ref tempRT2);
            }

            Blitter.BlitCameraTexture(cmd, tempRT1, source, material, (int)BlurPass.DualBlurUp);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Dispose();
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        
    }

    public void Dispose()
    {
        tempRT1?.Release();
        tempRT2?.Release();
        CoreUtils.Destroy(material);
    }
}


