using PostProcessSystem.SystemCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/ScanLine Jitter")]
    public class ScanlineJitter : global::PostProcessSystem.SystemCore.PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);

        public VolumeParameter<ScanLineJitterMode> jitterDirection = new VolumeParameter<ScanLineJitterMode>()
            { value = ScanLineJitterMode.Horizontal };
        public VolumeParameter<IntervalType> intervalType = new VolumeParameter<IntervalType>()
            { value = IntervalType.Random };
        public ClampedFloatParameter frequency = new ClampedFloatParameter(1f, 0, 25f);
        public ClampedFloatParameter jitterIntensity = new ClampedFloatParameter(0.5f, 0, 1f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 5;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Glitch";
        private int scanLineJitterParamsID = Shader.PropertyToID("_ScanLineJitterParams");
        private float randomFrequency;
        public override void Setup()
        {
            if (IsActive())
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shaderName);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UpdateFrequency();
            float displacement = 0.005f + Mathf.Pow(jitterIntensity.value, 3) * 0.1f;
            float threshold = Mathf.Clamp01(1.0f - jitterIntensity.value * 1.2f);
            material.SetVector(scanLineJitterParamsID,
                new Vector4(displacement, threshold,
                    intervalType.value == IntervalType.Random ? randomFrequency : frequency.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)jitterDirection.value);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
        void UpdateFrequency()
        {
            if (intervalType.value == IntervalType.Random)
            {
                randomFrequency = UnityEngine.Random.Range(0, frequency.value);
            }

            if (intervalType.value == IntervalType.Infinite)
            {
                material.EnableKeyword("USING_FREQUENCY_INFINITE");
            }
            else
            {
                material.DisableKeyword("USING_FREQUENCY_INFINITE");
            }
        }
    }
}

public enum ScanLineJitterMode
{
    Horizontal=GlitchPass.ScanLineJitterHorizontal,
    Vertical=GlitchPass.ScanLineJitterVertical,
}