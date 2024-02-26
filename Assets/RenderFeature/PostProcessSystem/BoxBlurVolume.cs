using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.PostProcessSystem
{
    [VolumeComponentMenu("DZ Post Processing/Blur/Box Blur")]
    public class BoxBlur: MyPostProcessing
    {
        public BoolParameter enableEffect = new BoolParameter(true);
        public ClampedIntParameter downScale = new ClampedIntParameter(1,1,4); 
        public ClampedIntParameter blurTimes = new ClampedIntParameter(0,0,5); 
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(0.0f,0f,4);
        public ClampedFloatParameter mipmap = new ClampedFloatParameter(0f, 0f,9);
        
        public override bool IsActive() => material != null && enableEffect == true && blurTimes.value != 0 && blurRadius.value!=0;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 102;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private string shaderName="MyURPShader/ShaderURPPostProcessing";
        private RTHandle BoxBlurTex1;
        private RTHandle BoxBlurTex2;
        private int boxBlurParams = Shader.PropertyToID("_BoxBlurParams");
        public override void Setup()
        {
            material=CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.useMipMap = true;
            descriptor.height /= downScale.value;
            descriptor.width /= downScale.value;
            
            RenderingUtils.ReAllocateIfNeeded(ref BoxBlurTex1, in descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp,
                name: "_BoxBlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref BoxBlurTex2, in descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp,
                name: "_BoxBlurTex2");
            material.SetVector(boxBlurParams,new Vector4( blurRadius.value,0,0,mipmap.value));
           
            Blitter.BlitCameraTexture(cmd, source, BoxBlurTex1, bilinear: true);
            for (int i = 0; i < blurTimes.value; i++)
            {
                Blitter.BlitCameraTexture(cmd, BoxBlurTex1, BoxBlurTex2, material,
                    (int)PostStackPass.BoxBlur);
                Blitter.BlitCameraTexture(cmd, BoxBlurTex2, BoxBlurTex1, material,
                    (int)PostStackPass.BoxBlur);
            }

            Blitter.BlitCameraTexture(cmd, BoxBlurTex1, dest,bilinear: true);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            BoxBlurTex1?.Release();
            BoxBlurTex2?.Release();
        }
    }
}