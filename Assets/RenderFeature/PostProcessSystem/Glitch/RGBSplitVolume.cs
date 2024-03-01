using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/RGB Split")]
    public class RGBSplit : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public VolumeParameter<SplitMode> mode = new VolumeParameter<SplitMode>();
        public ClampedFloatParameter fading = new ClampedFloatParameter(1f, 0, 1.0f);
        public ClampedFloatParameter amount = new ClampedFloatParameter(1f, 0, 5.0f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(2f, 0f, 10f);
        public ClampedFloatParameter centerFading = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter amountR = new ClampedFloatParameter(1f, 0f, 5.0f);
        public ClampedFloatParameter amountB = new ClampedFloatParameter(1f, 0f, 5.0f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 1;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private float timeX = 1.0f;
        private const string shaderName = "MyURPShader/URP_PostProcessing_Glitch";
        private int rgbSplitParamsID = Shader.PropertyToID("_RGBSplitParams");
        private int rgbSplitParams2ID = Shader.PropertyToID("_RGBSplitParams2");

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

            material.SetVector(rgbSplitParamsID,
                new Vector4(fading.value, amount.value, speed.value, centerFading.value));
            material.SetVector(rgbSplitParams2ID, new Vector4(timeX, amountR.value, amountB.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, dest, material, (int)mode.value);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}

public enum SplitMode
{
    Horizontal=GlitchPass.RGBSplitHorizontal,
    Vertical=GlitchPass.RGBSplitVertical,
    Combine=GlitchPass.RGBSplitCombine,
}