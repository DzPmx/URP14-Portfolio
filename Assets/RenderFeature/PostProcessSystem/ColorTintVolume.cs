using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature
{
    [VolumeComponentMenu("Custom Post-Processing/Color Tint")]
    public class ColorTintTest: MyPostProcessing
    {
        private BoolParameter enableEffect=new (true);
        private Material material;
        private const string shaderName = "MyURPShader/ShaderURPPostProcessing";
        public ColorParameter colorParameter = new ColorParameter(Color.white);
        private int colorTintID = Shader.PropertyToID("_ColorTint");
        private RTHandle GrabTex;
        public override bool IsActive() => material != null && enableEffect==true;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;
        public override int OrderInInjectionPoint => 0;
        public override void Setup()
        {
            material=CoreUtils.CreateEngineMaterial(shaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref GrabTex, descriptor, name: "_GrabTex");
            cmd.SetGlobalTexture("_GrabTexture", GrabTex.nameID);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            if (material ==null)return;
            material.SetColor(colorTintID,colorParameter.value);
            Blitter.BlitCameraTexture(cmd, source, GrabTex);
            CoreUtils.SetRenderTarget(cmd, dest);
            cmd.DrawProcedural(Matrix4x4.identity, material, (int)PostStackPass.ColorTint, MeshTopology.Triangles, 3);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            GrabTex?.Release();
        }
    }
}