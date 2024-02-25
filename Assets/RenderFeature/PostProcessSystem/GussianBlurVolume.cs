using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Gussian Blur")]
    public class GussianBlur : MyPostProcessing
    {
        public BoolParameter enableEffect = new(true);
        public ClampedIntParameter downScale = new ClampedIntParameter(2,1,4); 
        public ClampedIntParameter blurTimes = new ClampedIntParameter(1,0,5); 
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.5f,0f,4);
        public ClampedFloatParameter mipmap = new ClampedFloatParameter(2f, 0f,9);

        public override bool IsActive() => material != null && enableEffect == true;
        public override bool IsTileCompatible() => false;

        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;
        public override int OrderInInjectionPoint => 101;
        private const string shaderName = "MyURPShader/ShaderURPPostProcessing";
        private Material material;
        private RTHandle gussianBlurTex1;
        private RTHandle gussianBlurTex2;
        private int gussianBlurParams = Shader.PropertyToID("_GussianBlurParams");

        public override void Setup()
        {
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.useMipMap = true;
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;

            RenderingUtils.ReAllocateIfNeeded(ref gussianBlurTex1, in descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp,
                name: "_GussianBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref gussianBlurTex2, in descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp,
                name: "_GussianBlurTex2");

            Blitter.BlitCameraTexture(cmd, source, gussianBlurTex1);
            for (int i = 0; i < blurTimes.value; i++)
            {
                material.SetVector(gussianBlurParams,new Vector4( blurRadius.value,0,0,mipmap.value));
                Blitter.BlitCameraTexture(cmd, gussianBlurTex1, gussianBlurTex2, material,
                    (int)PostStackPass.GussianBlurHorizontal);
                Blitter.BlitCameraTexture(cmd, gussianBlurTex2, gussianBlurTex1, material,
                    (int)PostStackPass.GussianBlurVertical);
            }

            Blitter.BlitCameraTexture(cmd, gussianBlurTex1, dest);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            gussianBlurTex1?.Release();
            gussianBlurTex2?.Release();
        }
    }
}