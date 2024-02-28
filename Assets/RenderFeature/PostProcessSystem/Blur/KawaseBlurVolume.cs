using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Kawase Blur")]
    public class KawaseBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(2, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(2, 0, 8);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1.5f, 0f, 4f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 103;

        private Material material;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private RTHandle kawaseBlurTex1;
        private RTHandle kawaseBlurTex2;
        private int kawaseBlurParamsID = Shader.PropertyToID("_KawasePixelOffset");

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
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref kawaseBlurTex1, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_KawaseBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref kawaseBlurTex2, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_KawaseBlurTex2");
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, kawaseBlurTex1, bilinear: true);
            float radius = 0;
            for (int i = 0; i < blurTimes.value; i++)
            {
                material.SetFloat(kawaseBlurParamsID, blurRadius.value + radius);
                radius += 0.5f;
                Blitter.BlitCameraTexture(cmd, kawaseBlurTex1, kawaseBlurTex2, material, (int)BlurPass.KawaseBlur);
                Blitter.BlitCameraTexture(cmd, kawaseBlurTex2, kawaseBlurTex1, material, (int)BlurPass.KawaseBlur);
            }

            Blitter.BlitCameraTexture(cmd, kawaseBlurTex1, dest, bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            kawaseBlurTex1?.Release();
            kawaseBlurTex2?.Release();
        }
    }
}