using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OIT
{
    public class DepthPeelingOITRenderPass : ScriptableRenderPass
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
        private CullMode cullMode;

        public void SetUp(RTHandle color, RTHandle depth, OITSettings oitSettings)
        {
            sourceColorRT = color;
            sourceDepthRT = depth;
            this.layers = oitSettings.layers;
            initializeShader = oitSettings.DP_initialShader;
            depthPeelingShader = oitSettings.DP_depthPeelingShader;
            blendShader = oitSettings.DP_blendShader;
            this.cullMode = oitSettings.cullMode;
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

            this.descriptor.depthBufferBits = 32;
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

            CommandBuffer buffer = CommandBufferPool.Get("DP_InitializeTransparent");
            CullingResults cullingResults = renderingData.cullResults;
            DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("OrderdTransparent"),
                ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            drawingSettings.overrideShader = initializeShader;
            RenderStateBlock renderStateBlock = new RenderStateBlock();
            renderStateBlock.mask = RenderStateMask.Raster;
            renderStateBlock.rasterState = new RasterState(cullMode);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            //2 is DP
            filteringSettings.renderingLayerMask = 2;


            using (new ProfilingScope(buffer, new ProfilingSampler("DP_Initialize Transparent")))
            {
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
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
                CommandBuffer depthPeelingbuffer = CommandBufferPool.Get("DP_DepthPeeling " + i);
                mrtid[0] = colorRT[i].nameID;
                mrtid[1] = depthRT[i % 2].nameID;

                CoreUtils.SetRenderTarget(depthPeelingbuffer, mrtid, depthID);
                CoreUtils.ClearRenderTarget(depthPeelingbuffer, ClearFlag.All, new Color(1.0f, 1.0f, 1.0f, 0.0f));
                depthPeelingbuffer.SetGlobalTexture("_PrevDepthTex", depthRT[1 - i % 2]);
                using (new ProfilingScope(depthPeelingbuffer, new ProfilingSampler("DP_Depth Peeling")))
                {
                    context.ExecuteCommandBuffer(depthPeelingbuffer);
                    depthPeelingbuffer.Clear();
                    context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings,
                        ref renderStateBlock);
                }

                context.ExecuteCommandBuffer(depthPeelingbuffer);
                depthPeelingbuffer.Clear();
                depthPeelingbuffer.Dispose();
            }

            #endregion

            #region Blend

            CommandBuffer blendBuffer = CommandBufferPool.Get("DP_BlendTransparent");
            using (new ProfilingScope(blendBuffer, new ProfilingSampler("DP_Blend Transparent")))
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

        public void Dispose(bool disposing)
        {
            if (disposing)
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
}