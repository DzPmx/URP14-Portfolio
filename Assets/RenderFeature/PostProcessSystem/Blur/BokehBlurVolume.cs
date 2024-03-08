using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Bokeh Blur")]
    public class BokehBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter downScale = new ClampedIntParameter(10, 1, 10);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(50, 0, 128);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.25f, 0f, 10f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 105;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private RTHandle bokehBlurTex;
        private int bokehBlurParamsID = Shader.PropertyToID("_BokehBlurParams");
        private int goldenRotID = Shader.PropertyToID("_GoldenRot");
        float c = Mathf.Cos(2.39996323f);
        float s = Mathf.Sin(2.39996323f);

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
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;
            RenderingUtils.ReAllocateIfNeeded(ref bokehBlurTex, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BokehBlurTex");
            material.SetVector(goldenRotID, new Vector4(c, s, -s, c));
            material.SetVector(bokehBlurParamsID, new Vector4(blurTimes.value, blurRadius.value, 0, 0));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,bokehBlurTex,material,(int)BlurPass.BokehBlur);
            Blitter.BlitCameraTexture(cmd,bokehBlurTex,dest,material,(int)BlurPass.BokehBlur);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            bokehBlurTex?.Release();
        }
    }
}