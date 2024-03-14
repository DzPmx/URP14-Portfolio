using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Image Block")]
    public class ImageBlock : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedFloatParameter speed = new ClampedFloatParameter(10, 0, 50f);
        public ClampedFloatParameter blockSize = new ClampedFloatParameter(8f, 0, 50.0f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 2;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Glitch";
        private int imageBlockParamsID = Shader.PropertyToID("_ImageBlockParams");

        public override void Setup()
        {
            if (IsActive())
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shaderName);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            material.SetVector(imageBlockParamsID,new Vector4(speed.value,blockSize.value));
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)GlitchPass.ImageBlock);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}
