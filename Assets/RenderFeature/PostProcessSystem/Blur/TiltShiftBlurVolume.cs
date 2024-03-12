using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Blur
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Tilt Shift Blur")]
    public class TiltShiftBlur : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(60, 0, 128);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(4f, 0f, 10f);

        public ClampedFloatParameter areaSize = new ClampedFloatParameter(2f, 0f, 20f);
        public ClampedFloatParameter centerOffset = new ClampedFloatParameter(0f, -1f, 1f);
        public ClampedFloatParameter areaSmooth = new ClampedFloatParameter(0.8f, 0f, 20f);
        public BoolParameter debug = new BoolParameter(false);

        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 106;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Blur";
        private int tiltBokehBlurParamsID = Shader.PropertyToID("_TiltBokehBlurParams");
        private int goldenRotID = Shader.PropertyToID("_GoldenRot");
        private int gradientID = Shader.PropertyToID("_TiltShiftBlurGradient");
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
            material.SetVector(gradientID,new Vector4(centerOffset.value,areaSize.value,areaSmooth.value));
            material.SetVector(tiltBokehBlurParamsID, new Vector4(blurTimes.value, blurRadius.value, 0, 0));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,debug==true?(int)BlurPass.TiltShiftBokehBlurDebug:(int)BlurPass.TiltShiftBlur);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}