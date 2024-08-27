using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OIT
{
    public class WeightedBlendOITRenderPass : ScriptableRenderPass
    {
        private OITSettings oitSettings;
        private RTHandle BlitRT;
        private RTHandle sourceColor;
        private RTHandle sourceDepth;
        private RTHandle accumRT;
        private RTHandle revealageRT;

        private Shader accumulateShader;
        private Shader revealageShader;
        private Shader blendShader;

        private Material blendMaterial;

        private CullMode cullMode;

        public void SetUp(RTHandle color, RTHandle depth, OITSettings oitSettings)
        {
            sourceColor = color;
            sourceDepth = depth;
            this.oitSettings = oitSettings;

            accumulateShader = oitSettings.WB_accumulateShader;
            revealageShader = oitSettings.WB_revealageShader;
            blendShader = oitSettings.WB_blendShader;
            this.cullMode = oitSettings.cullMode;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.sRGB = false;
            RenderingUtils.ReAllocateIfNeeded(ref accumRT, descriptor, name: "accumulateRT");
            descriptor.graphicsFormat = GraphicsFormat.R16_SFloat;
            RenderingUtils.ReAllocateIfNeeded(ref revealageRT, descriptor, name: "revealageRT");
            descriptor.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            RenderingUtils.ReAllocateIfNeeded(ref BlitRT, descriptor, name: "BlitRT");
            if (blendMaterial == null)
            {
                blendMaterial = CoreUtils.CreateEngineMaterial(blendShader);
            }

            SwitchShaderKeyworld();
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.SetGlobalTexture("_AccumulateRT", accumRT);
            cmd.SetGlobalTexture("_RevealageRT", revealageRT);
            cmd.SetGlobalTexture("_WB_BlitRT", BlitRT);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            #region BlitColor

            CommandBuffer blitBuffer = CommandBufferPool.Get("WB_BlitColor");
            using (new ProfilingScope(blitBuffer, new ProfilingSampler("WB_Blit Color")))
            {
                Blitter.BlitCameraTexture(blitBuffer, sourceColor, BlitRT);
            }

            context.ExecuteCommandBuffer(blitBuffer);
            blitBuffer.Clear();
            blitBuffer.Dispose();

            #endregion


            CommandBuffer accumulateBuffer = CommandBufferPool.Get("WB_Accumulate");
            CoreUtils.SetRenderTarget(accumulateBuffer, accumRT, sourceDepth);
            CoreUtils.ClearRenderTarget(accumulateBuffer, ClearFlag.Color, Color.clear);

            CullingResults cullingResults = renderingData.cullResults;
            DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("OrderdTransparent"),
                ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            drawingSettings.overrideShader = accumulateShader;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            //4 is WB
            filteringSettings.renderingLayerMask = 4;

            RenderStateBlock renderStateBlock = new RenderStateBlock();
            renderStateBlock.mask = RenderStateMask.Raster;
            renderStateBlock.rasterState = new RasterState(cullMode);

            #region Render Accumulate

            using (new ProfilingScope(accumulateBuffer, new ProfilingSampler("WB_Render Accumulate ")))
            {
                context.ExecuteCommandBuffer(accumulateBuffer);
                accumulateBuffer.Clear();
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            }

            context.ExecuteCommandBuffer(accumulateBuffer);
            accumulateBuffer.Clear();
            accumulateBuffer.Dispose();

            #endregion

            CommandBuffer revealageBuffer = CommandBufferPool.Get("WB_Revealage");
            CoreUtils.SetRenderTarget(revealageBuffer, revealageRT, sourceDepth);
            CoreUtils.ClearRenderTarget(revealageBuffer, ClearFlag.Color, Color.white);
            drawingSettings.overrideShader = revealageShader;

            #region Render Revealage

            using (new ProfilingScope(revealageBuffer, new ProfilingSampler("WB_Render Revealage")))
            {
                context.ExecuteCommandBuffer(revealageBuffer);
                revealageBuffer.Clear();
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            }

            context.ExecuteCommandBuffer(revealageBuffer);
            revealageBuffer.Clear();
            revealageBuffer.Dispose();

            #endregion

            CommandBuffer blendBuffer = CommandBufferPool.Get("WB_Blend");
            CoreUtils.SetRenderTarget(blendBuffer, sourceColor, sourceDepth);
            using (new ProfilingScope(blendBuffer, new ProfilingSampler("WB_Render Blend")))
            {
                CoreUtils.DrawFullScreen(blendBuffer, blendMaterial);
            }

            context.ExecuteCommandBuffer(blendBuffer);
            blendBuffer.Clear();
            blendBuffer.Dispose();
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                accumRT?.Release();
                revealageRT?.Release();
                BlitRT?.Release();
            }
        }

        internal void SwitchShaderKeyworld()
        {
            switch (oitSettings.WeightFunction)
            {
                case WeightFunction.NoWeighted:
                    Shader.EnableKeyword("_No_WEIGHTED");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_1");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_2");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_3");
                    break;
                case WeightFunction.Function1:
                    Shader.EnableKeyword("_WEIGHTED_FUNTION_1");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_2");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_3");
                    Shader.DisableKeyword("_No_WEIGHTED");
                    break;
                case WeightFunction.Function2:
                    Shader.EnableKeyword("_WEIGHTED_FUNTION_2");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_1");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_3");
                    Shader.DisableKeyword("_No_WEIGHTED");
                    break;
                case WeightFunction.Function3:
                    Shader.EnableKeyword("_WEIGHTED_FUNTION_3");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_1");
                    Shader.DisableKeyword("_WEIGHTED_FUNTION_2");
                    Shader.DisableKeyword("_No_WEIGHTED");
                    break;
            }
        }
    }
}