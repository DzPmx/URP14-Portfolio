using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Wave Jitter")]
    public class WaveJitter : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public VolumeParameter<WaveJitterMode> waveJitterMode = new VolumeParameter<WaveJitterMode>()
            { value = WaveJitterMode.Horizontal };
        public VolumeParameter<IntervalType> intervalType = new VolumeParameter<IntervalType>()
            { value = IntervalType.Random };
        public ClampedFloatParameter frequency = new ClampedFloatParameter(30f, 0, 50f);
        public ClampedFloatParameter rgbSplit = new ClampedFloatParameter(20f, 0, 50f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(0.1f, 0, 1f);
        public ClampedFloatParameter amount = new ClampedFloatParameter(1f, 0, 2f);
        public BoolParameter customResolution = new BoolParameter(false);
        public Vector2Parameter resolution = new Vector2Parameter(new Vector2(640f, 480f));
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 8;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Glitch";
        private int waveJitterParamsID = Shader.PropertyToID("_WaveJitterParams");
        private int waveJitterResolutionID = Shader.PropertyToID("_WaveJitterResolution");
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
            material.SetVector(waveJitterParamsID,
                new Vector4(intervalType.value== IntervalType.Random ? randomFrequency : frequency.value,
                    rgbSplit.value, speed.value ,amount.value));
            material.SetVector(waveJitterResolutionID,customResolution.value? resolution.value : new Vector4(Screen.width,Screen.height));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)waveJitterMode.value);
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

public enum WaveJitterMode
{
    Horizontal=GlitchPass.WaveJitterHorizontal,
    Vertical=GlitchPass.WaveJitterVertical,
}
