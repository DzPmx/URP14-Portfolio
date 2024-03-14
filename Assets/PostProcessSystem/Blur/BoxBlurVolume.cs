using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Box Blur")]
    public class BoxBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(2, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(1, 0, 5);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1.5f, 0f, 6f);
        public ClampedFloatParameter mipmap = new ClampedFloatParameter(1.5f, 0f, 9);

        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 102;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Blur";
        private RTHandle boxBlurTex1;
        private RTHandle boxBlurTex2;
        private int boxBlurParamsID = Shader.PropertyToID("_BoxBlurParams");

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

            RenderingUtils.ReAllocateIfNeeded(ref boxBlurTex1, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_BoxBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref boxBlurTex2, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_BoxBlurTex2");
            material.SetVector(boxBlurParamsID, new Vector4(blurRadius.value, 0, 0, mipmap.value));

        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, boxBlurTex1, bilinear: true);
            for (int i = 0; i < blurTimes.value; i++)
            {
                Blitter.BlitCameraTexture(cmd, boxBlurTex1, boxBlurTex2, material,
                    (int)BlurPass.BoxBlur);
                Blitter.BlitCameraTexture(cmd, boxBlurTex2, boxBlurTex1, material,
                    (int)BlurPass.BoxBlur);
            }

            Blitter.BlitCameraTexture(cmd, boxBlurTex1, dest, bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            boxBlurTex1?.Release();
            boxBlurTex2?.Release();
        }
    }
}