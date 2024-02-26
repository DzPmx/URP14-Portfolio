using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Kawase Blur")]
    public class KawaseBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(true);
        public ClampedIntParameter downScale = new ClampedIntParameter(1, 1, 4);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(0, 0, 8);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.0f, 0f, 4);
        public override bool IsActive() => enableEffect == true && material != null && blurTimes.value != 0;
        public override bool IsTileCompatible() => false;

        private Material material;
        private string shaderName = "MyURPShader/ShaderURPPostProcessing";
        private RTHandle KawaseBlurTex1;
        private RTHandle KawaseBlurTex2;
        private int KawaseBlurParams = Shader.PropertyToID("_KawasePixelOffset");

        public override void Setup()
        {
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
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
            Blitter.BlitCameraTexture(cmd, source, KawaseBlurTex1, bilinear: true);
            float radius = 0;
            for (int i = 0; i < blurTimes.value; i++)
            {
                material.SetFloat(KawaseBlurParams, blurRadius.value+radius);
                radius += 0.5f;
                Blitter.BlitCameraTexture(cmd, KawaseBlurTex1, KawaseBlurTex2, material, (int)PostStackPass.KawaseBlur);
                Blitter.BlitCameraTexture(cmd, KawaseBlurTex2, KawaseBlurTex1, material, (int)PostStackPass.KawaseBlur);
            }
            
            Blitter.BlitCameraTexture(cmd, KawaseBlurTex1, dest, material, (int)PostStackPass.KawaseBlur);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            KawaseBlurTex1?.Release();
            KawaseBlurTex2?.Release();
        }
    }
}