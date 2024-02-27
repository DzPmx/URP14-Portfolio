using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Dual Blur")]
    public class DualBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(true);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(0, 0, 5);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.0f, 0f, 4f);
        public override bool IsActive() => enableEffect == true && material != null && blurTimes.value != 0;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 104;

        private Material material;
        private string shaderName = "MyURPShader/ShaderURPPostProcessing";
        private RTHandle dualBlurTex1;
        private RTHandle dualBlurTex2;
        private int dualBlurParamsID = Shader.PropertyToID("_DualBlurOffset");

        public override void Setup()
        {
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref dualBlurTex1, in descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_DualBlurTex1");
            Blitter.BlitCameraTexture(cmd, source, dualBlurTex1, bilinear: true);
            material.SetFloat(dualBlurParamsID, blurRadius.value);
            int scaleTimes = 0;
            for (int i = 0; i < blurTimes.value; i++)
            {
                if (descriptor.height < 130)
                {
                    break;
                }

                descriptor.height /= 2;
                descriptor.width /= 2;
                RenderingUtils.ReAllocateIfNeeded(ref dualBlurTex2, in descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_DualBlurTex2");
                scaleTimes++;
                Blitter.BlitCameraTexture(cmd, dualBlurTex1, dualBlurTex2, material, (int)PostStackPass.DualBlurDown);
                CoreUtils.Swap(ref dualBlurTex1, ref dualBlurTex2);
            }

            for (int i = 0; i < scaleTimes - 1; i++)
            {
                descriptor.height *= 2;
                descriptor.width *= 2;
                RenderingUtils.ReAllocateIfNeeded(ref dualBlurTex2, in descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_DualBlurTex2");
                Blitter.BlitCameraTexture(cmd, dualBlurTex1, dualBlurTex2, material, (int)PostStackPass.DualBlurUp);
                CoreUtils.Swap(ref dualBlurTex1, ref dualBlurTex2);
            }

            Blitter.BlitCameraTexture(cmd, dualBlurTex1, dest, material, (int)PostStackPass.DualBlurUp);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            dualBlurTex1?.Release();
            dualBlurTex2?.Release();
        }
    }
}