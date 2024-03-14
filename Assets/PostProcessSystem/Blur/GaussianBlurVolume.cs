using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Gaussian Blur")]
    public class GaussianBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(2, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(1, 0, 5);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1.5f, 0f, 4f);
        public ClampedFloatParameter mipmap = new ClampedFloatParameter(1.5f, 0f, 9);

        public override bool IsActive() => enableEffect == true ;
        public override bool IsTileCompatible() => false;

        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;
        public override int OrderInInjectionPoint => 101;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Blur";
        private Material material;
        private RTHandle gaussianBlurTex1;
        private RTHandle gaussianBlurTex2;
        private int gaussianBlurParamsID = Shader.PropertyToID("_GaussianBlurParams");

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

            RenderingUtils.ReAllocateIfNeeded(ref gaussianBlurTex1, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_GaussianBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref gaussianBlurTex2, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_GaussianBlurTex2");
            material.SetVector(gaussianBlurParamsID, new Vector4(blurRadius.value, 0, 0, mipmap.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, gaussianBlurTex1);
            for (int i = 0; i < blurTimes.value; i++)
            {
                Blitter.BlitCameraTexture(cmd, gaussianBlurTex1, gaussianBlurTex2, material,
                    (int)BlurPass.GussianBlurHorizontal);
                Blitter.BlitCameraTexture(cmd, gaussianBlurTex2, gaussianBlurTex1, material,
                    (int)BlurPass.GussianBlurVertical);
            }

            Blitter.BlitCameraTexture(cmd, gaussianBlurTex1, dest);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            gaussianBlurTex1?.Release();
            gaussianBlurTex2?.Release();
        }
    }
}