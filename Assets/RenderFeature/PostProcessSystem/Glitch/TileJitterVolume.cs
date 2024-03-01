using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Tile Jitter")]
    public class TileJitter : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(false);

        public VolumeParameter<TileJitterMode> tileJitterMode = new VolumeParameter<TileJitterMode>()
            { value = TileJitterMode.Horizontal };
        public VolumeParameter<IntervalType> intervalType = new VolumeParameter<IntervalType>()
            { value = IntervalType.Random };
        public VolumeParameter<TileJitterMode> jitterDirection = new VolumeParameter<TileJitterMode>()
            { value = TileJitterMode.Horizontal };
        public ClampedFloatParameter frequency = new ClampedFloatParameter(7f, 0, 25f);
        public ClampedFloatParameter splittingNumber = new ClampedFloatParameter(12f, 0, 50f);
        public ClampedFloatParameter amount = new ClampedFloatParameter(40f, 0, 100f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(0.25f, 0, 1f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 4;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Glitch";
        private int tileJitterParamsID = Shader.PropertyToID("_TileJitterParams");
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
            if (jitterDirection.value == TileJitterMode.Horizontal)
            {
                material.EnableKeyword("JITTER_DIRECTION_HORIZONTAL");
            }
            else
            {
                material.DisableKeyword("JITTER_DIRECTION_HORIZONTAL");
            }
            material.SetVector(tileJitterParamsID,
                new Vector4(splittingNumber.value, amount.value, speed.value * 100f,
                    intervalType.value == IntervalType.Random ? randomFrequency : frequency.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)tileJitterMode.value);
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

public enum TileJitterMode
{
    Horizontal=GlitchPass.TileJitterHorizontal,
    Vertical=GlitchPass.TileJitterVertical,
}