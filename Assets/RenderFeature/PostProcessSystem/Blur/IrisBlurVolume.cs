using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Iris Blur")]
    public class IrisBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(32, 0, 128);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(4f, 0f, 10f);

        public ClampedFloatParameter areaSize = new ClampedFloatParameter(40f, 0f, 50f);
        public ClampedFloatParameter centerOffsetX = new ClampedFloatParameter(0f, -1f, 1f);
        public ClampedFloatParameter centerOffsetY = new ClampedFloatParameter(0f, -1f, 1f);
        public BoolParameter debug = new BoolParameter(false);

        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 107;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private int irisBokehBlurParamsID = Shader.PropertyToID("_IrisBokehBlurParams");
        private int goldenRotID = Shader.PropertyToID("_GoldenRot");
        private int irisBokehBlurgradientID = Shader.PropertyToID("_IrisBokehBlurGradient");
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
            material.SetVector(goldenRotID, new Vector4(c, s, -s, c));
            material.SetVector(irisBokehBlurgradientID, new Vector4(centerOffsetX.value, centerOffsetY.value, areaSize.value*0.1f));
            material.SetVector(irisBokehBlurParamsID, new Vector4(blurTimes.value, blurRadius.value, 0, 0));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,debug==true? (int)BlurPass.IrisBlurDebug:(int)BlurPass.IrisBlur);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}