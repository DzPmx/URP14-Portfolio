using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SSS.Burley_Normalized_Screen_Space_SSS
{
    public class BurleyNormalizedSSSFeature : ScriptableRendererFeature
    {
        BurleyNormalizedSSSPass burleyNormalizedSss;
        public BurleyNormalizedSSSSettings settings;

        public override void Create()
        {
            this.name = "Burley Normalized SSS";
            burleyNormalizedSss = new BurleyNormalizedSSSPass();
            burleyNormalizedSss.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            burleyNormalizedSss.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            burleyNormalizedSss.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(burleyNormalizedSss);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            burleyNormalizedSss.Dispose();
        }
    }

    internal class BurleyNormalizedSSSPass : ScriptableRenderPass
    {
        private BurleyNormalizedSSSSettings settings;
        static readonly int filterRadiiID = Shader.PropertyToID("_FilterRadii");
        static readonly int shapeParamsAndMaxScatterDistsID = Shader.PropertyToID("_ShapeParamsAndMaxScatterDists");
        static readonly int worldScaleID = Shader.PropertyToID("_WorldScale");
        static readonly int invProjectMatrixID = Shader.PropertyToID("_InvProjectMatrix");
        static readonly int blitTextureID = Shader.PropertyToID("_BlitTexture");
        private static readonly int burleyNormalizedSSSTextureID = Shader.PropertyToID("_BurleyNormalizedSSSTexture");
        private RTHandle sourceColor;
        private RTHandle sourceDepth;
        private RTHandle SSSTexture;

        private Material material;
        private ProfilingSampler blurSampler = new ProfilingSampler("Separable SSS Blur");
        private const string shaderName = "MyURPShader/Character Rendering/Burley Normalized SSS";


        private static ShaderTagId dualLobe = new ShaderTagId("Burley Normalized Dual Specular");


        public void Setup(RTHandle cameraColor, RTHandle cameraDepth, BurleyNormalizedSSSSettings settings)
        {
            sourceColor = cameraColor;
            sourceDepth = cameraDepth;
            this.settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref SSSTexture, descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_BurleySSSTexture1");

            if (material == null)
            {
                material = CoreUtils.CreateEngineMaterial(shaderName);
            }

            material.SetTexture(blitTextureID, sourceColor);

            //RGB散射距离,作为参数调节
            Color scatteringDistance = settings.scatteringDistance;
            Vector3 shapeParam = new Vector3(Mathf.Min(16777216, 1.0f / scatteringDistance.r),
                Mathf.Min(16777216, 1.0f / scatteringDistance.g),
                Mathf.Min(16777216, 1.0f / scatteringDistance.b));

            //通过0.997f的cdf计算出最大的散射范围
            float maxScatteringDistance = Mathf.Max(scatteringDistance.r, scatteringDistance.g, scatteringDistance.b);
            float cdf = 0.997f;
            float filterRadius = SampleBurleyDiffusionProfile(cdf, maxScatteringDistance);

            //次表面散射部分的参数
            material.SetFloat(filterRadiiID, filterRadius);
            material.SetVector(shapeParamsAndMaxScatterDistsID,
                new Vector4(shapeParam.x, shapeParam.y, shapeParam.z, maxScatteringDistance));
            var projectMatrix = renderingData.cameraData.camera.projectionMatrix;
            var invProject = projectMatrix.inverse; //p矩阵的逆矩阵
            material.SetFloat(worldScaleID, settings.worldScale);
            material.SetMatrix(invProjectMatrixID, invProject);
            cmd.SetGlobalTexture(burleyNormalizedSSSTextureID,SSSTexture);
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get("Burley Normalized SSS");
            using (new ProfilingScope(buffer, blurSampler))
            {
                CoreUtils.SetRenderTarget(buffer, SSSTexture, sourceDepth);
                CoreUtils.ClearRenderTarget(buffer, ClearFlag.Color, Color.grey);
                buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
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
            SSSTexture?.Release();
        }

        static float SampleBurleyDiffusionProfile(float u, float rcpS)
        {
            u = 1 - u; // Convert CDF to CCDF

            float g = 1 + (4 * u) * (2 * u + Mathf.Sqrt(1 + (4 * u) * u));
            float n = Mathf.Pow(g, -1.0f / 3.0f); // g^(-1/3)
            float p = (g * n) * n; // g^(+1/3)
            float c = 1 + p + n; // 1 + g^(+1/3) + g^(-1/3)
            float x = 3 * Mathf.Log(c / (4 * u));

            return x * rcpS;
        }
    }
}