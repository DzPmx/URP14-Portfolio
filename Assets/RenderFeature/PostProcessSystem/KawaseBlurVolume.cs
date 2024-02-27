using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Kawase Blur")]
    public class KawaseBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(true);
        public ClampedIntParameter downScale = new ClampedIntParameter(1, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(0, 0, 8);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.0f, 0f, 4f);
        public override bool IsActive() => enableEffect == true && material != null && blurTimes.value != 0;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 103;

        private Material material;
        private string shaderName = "MyURPShader/ShaderURPPostProcessing";
        private RTHandle KawaseBlurTex1;
        private RTHandle KawaseBlurTex2;
        private int kawaseBlurParamsID = Shader.PropertyToID("_KawasePixelOffset");

        public override void Setup()
        {
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref KawaseBlurTex1, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_KawaseBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref KawaseBlurTex2, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_KawaseBlurTex2");
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, KawaseBlurTex1, bilinear: true);
            float radius = 0;
            for (int i = 0; i < blurTimes.value; i++)
            {
                material.SetFloat(kawaseBlurParamsID, blurRadius.value + radius);
                radius += 0.5f;
                Blitter.BlitCameraTexture(cmd, KawaseBlurTex1, KawaseBlurTex2, material, (int)PostStackPass.KawaseBlur);
                Blitter.BlitCameraTexture(cmd, KawaseBlurTex2, KawaseBlurTex1, material, (int)PostStackPass.KawaseBlur);
            }

            Blitter.BlitCameraTexture(cmd, KawaseBlurTex1, dest, bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            KawaseBlurTex1?.Release();
            KawaseBlurTex2?.Release();
        }
    }
}