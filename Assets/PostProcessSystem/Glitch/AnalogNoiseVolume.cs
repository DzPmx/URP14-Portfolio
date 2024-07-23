using PostProcessSystem.SystemCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Analog Noise")]
    public class AnalogNoise : global::PostProcessSystem.SystemCore.PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedFloatParameter noiseSpeed = new ClampedFloatParameter(0.05f, 0f, 1f);
         public ClampedFloatParameter noiseFading = new ClampedFloatParameter(1f, 0, 1f);
        public ClampedFloatParameter luminanceJitterThreshold = new ClampedFloatParameter(0.8f, 0, 1.0f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 7;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private float timeX = 1.0f;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Glitch";
        private int analogNoiseParamsID = Shader.PropertyToID("_AnalogNoiseParams");

        public override void Setup()
        {
            if (IsActive())
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shaderName);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            timeX += Time.deltaTime;
            if (timeX > 100)
            {
                timeX = 0;
            }

            material.SetVector(analogNoiseParamsID,
                new Vector4(noiseSpeed.value, noiseFading.value, luminanceJitterThreshold.value, timeX));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, dest, material, (int)GlitchPass.AnalogNoise);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}
