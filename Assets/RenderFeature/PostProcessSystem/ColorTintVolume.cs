using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [Serializable, VolumeComponentMenu("DZ Post Processing/Color Tint")]
    public class ColorTint : MyPostProcessing
    {
        public BoolParameter enable = new(false);
        private Material material;
        private const string shaderName = "MyURPShader/ShaderURPPostProcessing";
        public ColorParameter colorParameter = new ColorParameter(Color.white);
        private int colorTintID = Shader.PropertyToID("_ColorTint");
        public override bool IsActive() => material != null && enable == true && colorParameter.value != Color.white;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.AfterPostProcess;
        public override int OrderInInjectionPoint => 10000;

        public override void Setup()
        {
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            material.SetColor(colorTintID, colorParameter.value);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd, source, dest, material, (int)PostStackPass.ColorTint);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
        }
    }
}