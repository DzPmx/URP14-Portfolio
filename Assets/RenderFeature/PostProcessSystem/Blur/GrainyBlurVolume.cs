using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Grainy Blur")]
    public class GrainyBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(2, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(3, 0, 8);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(6f, 0f, 50f);

        public override bool IsActive() =>  enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 108;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private RTHandle grainyBlurTex;
        private int grainyBlurParamsID = Shader.PropertyToID("_GrainyBlurParams");

        public override void Setup()
        {
            if (IsActive())
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shaderName);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.useMipMap = true;
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;
            
            RenderingUtils.ReAllocateIfNeeded(ref grainyBlurTex, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_GrainyBlurTex");
            material.SetVector(grainyBlurParamsID, new Vector4(blurRadius.value/descriptor.height, blurTimes.value, 0));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, grainyBlurTex, material,(int)BlurPass.GrainyBlur);

            Blitter.BlitCameraTexture(cmd, grainyBlurTex, dest, bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            grainyBlurTex?.Release();

        }
    }
}