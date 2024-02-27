using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Radial Blur")]
    public class RadialBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(true);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(0, 0, 30);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.5f, 0f, 1f);
        
        public ClampedFloatParameter radialCenterX = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter radialCenterY = new ClampedFloatParameter(0.5f, 0f, 1f);

       
        public override bool IsActive() => material != null && enableEffect == true && blurTimes.value != 0;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 109;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private string shaderName = "MyURPShader/URP_PostProcessing_Blur";
        private int radialBlurParamsID = Shader.PropertyToID("_RadialBlurParams");


        public override void Setup()
        {
            material=CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            material.SetVector(radialBlurParamsID, new Vector4(blurRadius.value*0.02f, blurTimes.value, radialCenterX.value, radialCenterY.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)BlurPass.RadialBlur);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}