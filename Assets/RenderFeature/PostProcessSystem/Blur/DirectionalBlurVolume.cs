using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Directional Blur")]
    public class DirectionalBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(3, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(12, 0, 30);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(2f, 0f, 5f);
        public ClampedFloatParameter angle = new ClampedFloatParameter(3f, 0f, 6f);
        public override bool IsActive() => enableEffect == true ;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 110;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private RTHandle directionalBlurTex;
        private int directionalBlurParamsID = Shader.PropertyToID("_DirectionalBlurParams");

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
            
            RenderingUtils.ReAllocateIfNeeded(ref directionalBlurTex, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_DirectionalBlurTex");
            float sinVal = (Mathf.Sin(angle.value) * blurRadius.value * 0.05f) / blurTimes.value;
            float cosVal = (Mathf.Cos(angle.value) * blurRadius.value * 0.05f) / blurTimes.value;   
            material.SetVector(directionalBlurParamsID, new Vector4(blurTimes.value, sinVal, cosVal));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, directionalBlurTex, material,(int)BlurPass.DirectionalBlur);
            Blitter.BlitCameraTexture(cmd, directionalBlurTex, dest, bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            directionalBlurTex?.Release();

        }
    }
}