using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SSS.Separable_SSS
{
    public class SeparableSSSFeature : ScriptableRendererFeature
    {
        public SeparableSSSSettings settings;
        SeparablSSSPass separablSSS;

        public override void Create()
        {
            this.name = "SeparableSSS";
            separablSSS = new SeparablSSSPass();
            separablSSS.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            separablSSS.settings = settings;
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            separablSSS.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            separablSSS.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(separablSSS);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            separablSSS.Dispose();
        }
    }

    internal class SeparablSSSPass : ScriptableRenderPass
    {
        static readonly int blitTextureID = Shader.PropertyToID("_BlitTexture");
        static readonly int sssTextureID = Shader.PropertyToID("_SeparableSSSTexture");
        static readonly int kernelID = Shader.PropertyToID("_Kernel");
        static readonly int sssScalerID = Shader.PropertyToID("_SSSScale");
        static readonly int noiseID = Shader.PropertyToID("_Noise");
        static readonly int jitterID = Shader.PropertyToID("_Jitter");
        static readonly int screenSizeID = Shader.PropertyToID("_screenSize");
        static readonly int noiseSizeID = Shader.PropertyToID("_NoiseSize");
        private RTHandle sourceColor;
        private RTHandle sourceDepth;
        private RTHandle SSSTexture1;
        private RTHandle SSSTexture2;
        private Material material;
        private List<Vector4> kernelArray = new List<Vector4>();
        private ProfilingSampler blurSampler = new ProfilingSampler("Separable SSS Blur");
        private const string shaderName = "MyURPShader/Character Rendering/SeparableSSS";
        public SeparableSSSSettings settings;
        private static ShaderTagId dualLobe = new ShaderTagId("Separable Dual Specular");

        public void Setup(RTHandle cameraColor, RTHandle cameraDepth)
        {
            sourceColor = cameraColor;
            sourceDepth = cameraDepth;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref SSSTexture1, descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_SeparableSSSTexture1");
            RenderingUtils.ReAllocateIfNeeded(ref SSSTexture2, descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_SeparableSSSTexture2");
            if (material == null)
            {
                material = CoreUtils.CreateEngineMaterial(shaderName);
            }
            
            material.SetVector(noiseSizeID, new Vector2(64, 64));
            Vector3 SSSC = Vector3.Normalize(new Vector3(settings.subsurfaceColor.r, settings.subsurfaceColor.g,
                settings.subsurfaceColor.b));
            Vector3 SSSFC = Vector3.Normalize(new Vector3(settings.subsurfaceFalloff.r, settings.subsurfaceFalloff.g,
                settings.subsurfaceFalloff.b));
            SeparableSSSLibrary.CalculateKernel(kernelArray, 25, SSSC, SSSFC);
            // Vector2 jitterSample = GenerateRandomOffset();
            // material.SetVector(jitterID,
            //     new Vector4((float)settings.blueNoise.width, (float)settings.blueNoise.height, jitterSample.x,
            //         jitterSample.y));
            material.SetVector(screenSizeID,
                new Vector4(renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight, 0,
                    0));
            material.SetVectorArray(kernelID, kernelArray);
            material.SetFloat(sssScalerID, settings.subsurfaceScaler);
            material.SetFloat("_RandomSeed", Random.Range(0, 100));
            material.SetTexture(blitTextureID, sourceColor);
            cmd.SetGlobalTexture(sssTextureID, SSSTexture2);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get("Separable SSS");
            using (new ProfilingScope(buffer, blurSampler))
            {
                CoreUtils.SetRenderTarget(buffer, SSSTexture1, sourceDepth);
                CoreUtils.ClearRenderTarget(buffer, ClearFlag.Color, Color.black);
                buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
                Blitter.BlitCameraTexture(buffer, SSSTexture1, SSSTexture2, material, 1);
                CoreUtils.SetRenderTarget(buffer, sourceColor, sourceDepth);
            }

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            CullingResults cullingResults = renderingData.cullResults;
            SortingSettings sortingSettings = new SortingSettings(renderingData.cameraData.camera);
            DrawingSettings drawingSettings = new DrawingSettings(dualLobe, sortingSettings);
            drawingSettings.perObjectData = PerObjectData.ReflectionProbes |
                                            PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume | PerObjectData.LightIndices |
                                            PerObjectData.LightData | PerObjectData.MotionVectors;
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            buffer.Clear();
            buffer.Dispose();
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            SSSTexture1?.Release();
            SSSTexture2?.Release();
        }

        private float GetHaltonValue(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / (float)radix;

            while (index > 0)
            {
                result += (float)(index % radix) * fraction;
                index /= radix;
                fraction /= (float)radix;
            }

            return result;
        }

        private int SampleCount = 64;
        private int SampleIndex = 0;

        private Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(GetHaltonValue(SampleIndex & 1023, 2), GetHaltonValue(SampleIndex & 1023, 3));
            if (SampleIndex++ >= SampleCount)
                SampleIndex = 0;
            return offset;
        }
    }
}