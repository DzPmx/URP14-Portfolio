using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Line Block")]
    public class LineBlock : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public VolumeParameter<LineBlockMode> lineBlockMode = new VolumeParameter<LineBlockMode>(){value = LineBlockMode.Horizontal};
        public VolumeParameter<IntervalType> intervalType = new VolumeParameter<IntervalType>();
        public ClampedFloatParameter frequency = new ClampedFloatParameter(1f, 0, 25f);
        public ClampedFloatParameter amount = new ClampedFloatParameter(0.5f, 0, 1f);
        public ClampedFloatParameter linesWidth = new ClampedFloatParameter(1f, 0.1f, 10f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(0.1f, 0f, 1f);
        public ClampedFloatParameter offset = new ClampedFloatParameter(1f, 0f, 12f);
        public ClampedFloatParameter alpha = new ClampedFloatParameter(1f, 0f, 1f);
        public override bool IsActive() => enableEffect == true ;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 3;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private float timeX = 1.0f;
        private float randomFrequency;
        private int frameCount = 0;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Glitch";
        private int lineBlockParamsID = Shader.PropertyToID("_LineBlockParams");
        private int lineBlockParams2ID = Shader.PropertyToID("_LineBlockParams2");

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
            timeX += Time.deltaTime;
            if (timeX > 100)
            {
                timeX = 0;
            }

            material.SetVector(lineBlockParamsID,
                new Vector4(intervalType.value==IntervalType.Random? randomFrequency: frequency.value,
                    timeX*speed.value*0.2f, amount.value));
            material.SetVector(lineBlockParams2ID, new Vector4(offset.value, 1/linesWidth.value, alpha.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, dest, material,(int)lineBlockMode.value);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }

        void UpdateFrequency()
        {
            if (intervalType.value == IntervalType.Random)
            {
                if (frameCount > frequency.value)
                {

                    frameCount = 0;
                    randomFrequency = UnityEngine.Random.Range(0,frequency.value);
                }

                frameCount++;
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

public enum LineBlockMode
{
    Horizontal=GlitchPass.LineBlockHorizontal,
    Vertical=GlitchPass.LineBlockVertical,
}

