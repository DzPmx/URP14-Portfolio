using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OIT.DepthPeeling
{
    public class DepthPeelingRenderPass : ScriptableRenderPass
    {
        private int layers;
        private RTHandle sourceColorRT;
        private RTHandle sourceDepthRT;
        private RTHandle BlendRT;
        private RTHandle[] colorRT = new RTHandle[6];
        private RTHandle[] depthRT = new RTHandle[2];
        private RTHandle[] mrt = new RTHandle[2];
        RenderTargetIdentifier[] mrtid = new RenderTargetIdentifier[2];
        private Shader initializeShader;
        private Shader depthPeelingShader;
        private Shader blendShader;
        private Material BlendMaterial;
        private RenderTextureDescriptor descriptor;

        public void SetUp(RTHandle color, RTHandle depth, OITSettings oitSettings)
        {
            sourceColorRT = color;
            sourceDepthRT = depth;
            this.layers = oitSettings.layers;
            initializeShader = oitSettings.initialShader;
            depthPeelingShader = oitSettings.depthPeelingShader;
            blendShader = oitSettings.blendShader;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.depthBufferBits = 0;


            for (int i = 0; i < layers; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref colorRT[i], in descriptor, name: "TransparentColorRT " + i);
            }

            RenderingUtils.ReAllocateIfNeeded(ref depthRT[0], in descriptor, name: "TransparentDepthRT " + 0);
            RenderingUtils.ReAllocateIfNeeded(ref depthRT[1], in descriptor, name: "TransparentDepthRT " + 1);
            RenderingUtils.ReAllocateIfNeeded(ref BlendRT, in descriptor, name: "OpaqueDepthRT");

            if (BlendMaterial == null)
            {
                BlendMaterial = CoreUtils.CreateEngineMaterial(blendShader);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            mrt[0] = colorRT[0];
            mrt[1] = depthRT[0];
            ConfigureTarget(mrt, sourceDepthRT);
            ConfigureClear(ClearFlag.Color, new Color(1.0f, 1.0f, 1.0f, 0.0f));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            #region InitializeTransparent

            CommandBuffer buffer = CommandBufferPool.Get("InitializeTransparent");
            CullingResults cullingResults = renderingData.cullResults;
            SortingSettings sortingSettings = new SortingSettings(renderingData.cameraData.camera);
            DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("OrderdTrasparent"), sortingSettings);
            drawingSettings.overrideShader = initializeShader;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);


            using (new ProfilingScope(buffer, new ProfilingSampler("Initialize Transparent")))
            {
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.Dispose();

            #endregion

            #region Depth Peeling

            RenderTargetIdentifier depthID = sourceDepthRT;
            drawingSettings.overrideShader = depthPeelingShader;
            for (int i = 1; i < layers; i++)
            {
                CommandBuffer depthPeelingbuffer = CommandBufferPool.Get("DepthPeeling " + i);
                mrtid[0] = colorRT[i].nameID;
                mrtid[1] = depthRT[i % 2].nameID;

                CoreUtils.SetRenderTarget(depthPeelingbuffer, mrtid, depthID);
                CoreUtils.ClearRenderTarget(depthPeelingbuffer, ClearFlag.All, new Color(1.0f, 1.0f, 1.0f, 0.0f));
                depthPeelingbuffer.SetGlobalTexture("_PrevDepthTex", depthRT[1 - i % 2]);
                using (new ProfilingScope(depthPeelingbuffer, new ProfilingSampler("Depth Peeling")))
                {
                    context.ExecuteCommandBuffer(depthPeelingbuffer);
                    depthPeelingbuffer.Clear();
                    context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
                }

                context.ExecuteCommandBuffer(depthPeelingbuffer);
                depthPeelingbuffer.Clear();
                depthPeelingbuffer.Dispose();
            }

            #endregion

            #region Blend

            CommandBuffer blendBuffer = CommandBufferPool.Get("BlendTransparent");
            using (new ProfilingScope(blendBuffer, new ProfilingSampler("Blend Transparent")))
            {
                context.ExecuteCommandBuffer(blendBuffer);
                blendBuffer.Clear();
                Blitter.BlitCameraTexture(blendBuffer, sourceColorRT, depthRT[0]);
                for (int i = layers - 1; i >= 0; i--)
                {
                    blendBuffer.SetGlobalTexture("_LayerTex", colorRT[i]);
                    Blitter.BlitCameraTexture(blendBuffer, depthRT[0], depthRT[1], BlendMaterial, 0);
                    CoreUtils.Swap(ref depthRT[0], ref depthRT[1]);
                }

                Blitter.BlitCameraTexture(blendBuffer, depthRT[0], sourceColorRT);
            }

            context.ExecuteCommandBuffer(blendBuffer);
            blendBuffer.Clear();
            blendBuffer.Dispose();

            #endregion
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            for (int i = 0; i < 2; i++)
            {
                depthRT[i]?.Release();
                mrt[i]?.Release();
            }

            for (int i = 0; i < layers; i++)
            {
                colorRT[i]?.Release();
            }

            BlendRT?.Release();
        }
    }
}