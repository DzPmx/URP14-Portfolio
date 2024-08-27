using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OIT
{
    public class PerPixelLinkedListRenderPass : ScriptableRenderPass
    {
        private const int MAX_SORTED_PIXELS = 16;

        private readonly int clearStartOffsetBufferKernel;
        private ComputeShader clearStartOffsetBuffer;
        private ComputeBuffer startOffsetBuffer;
        private ComputeBuffer fragLinkedBuffer;

        private static readonly int ScreenWidth = Shader.PropertyToID("screenWidth");
        private static readonly int startOffsetBufferID = Shader.PropertyToID("startOffetBuffer");
        private static readonly int fragLinkedBufferID = Shader.PropertyToID("fragLinkedBuffer");

        private Material linkedListMaterial;

        private RTHandle sourceColor;
        private RTHandle sourceDepth;
        private RTHandle blitRT;

        private CullMode cullMode;

        private int blitRTID = Shader.PropertyToID("_PPLL_BlitRT");

        public Shader PPLL_Initial;
        public Shader PPLL_Blend;


        public PerPixelLinkedListRenderPass()
        {
            clearStartOffsetBuffer = Resources.Load<ComputeShader>("ClearStartOffetBuffer");
            clearStartOffsetBufferKernel = clearStartOffsetBuffer.FindKernel("ClearStartOffset");
            RenderPipelineManager.beginContextRendering += RenderCSBuffer;
        }

        public void SetUp(RTHandle color, RTHandle depth, OITSettings oitSettings)
        {
            sourceColor = color;
            sourceDepth = depth;
            this.cullMode = oitSettings.cullMode;
            this.PPLL_Initial = oitSettings.PPLL_BlendShader;
            this.PPLL_Blend = oitSettings.PPLL_BlendShader;
        }

        public void RenderCSBuffer(ScriptableRenderContext context, List<Camera> cameras)
        {
            CommandBuffer renderCSBuffer = CommandBufferPool.Get("Render CS Buffer");

            int bufferSize = Screen.width * Screen.height * MAX_SORTED_PIXELS;
            int bufferStride = sizeof(uint) * 3;


            if (fragLinkedBuffer == null)
            {
                fragLinkedBuffer = new ComputeBuffer(bufferSize, bufferStride, ComputeBufferType.Counter);
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            int bufferSizeHead = Screen.width * Screen.height * MAX_SORTED_PIXELS;
            int bufferStrideHead = sizeof(uint) * 3;

            if (startOffsetBuffer == null)
            {
                startOffsetBuffer = new ComputeBuffer(bufferSizeHead, bufferStrideHead, ComputeBufferType.Raw);
            }

            clearStartOffsetBuffer.SetBuffer(clearStartOffsetBufferKernel, startOffsetBufferID, startOffsetBuffer);
            clearStartOffsetBuffer.SetInt(ScreenWidth, Screen.width);


            int dispatchGroupSizeX = Mathf.CeilToInt(screenWidth / 32.0f);
            int dispatchGroupSizeY = Mathf.CeilToInt(screenHeight / 32.0f);

            clearStartOffsetBuffer.Dispatch(clearStartOffsetBufferKernel, dispatchGroupSizeX, dispatchGroupSizeY, 1);

            renderCSBuffer.SetRandomWriteTarget(1, fragLinkedBuffer);
            renderCSBuffer.SetRandomWriteTarget(2, startOffsetBuffer);

            context.ExecuteCommandBuffer(renderCSBuffer);

            renderCSBuffer.Clear();
            renderCSBuffer.Dispose();
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref blitRT, descriptor, name: "_BlitRT");
            cmd.SetGlobalTexture(blitRTID, blitRT);
            if (linkedListMaterial == null)
            {
                linkedListMaterial = CoreUtils.CreateEngineMaterial(PPLL_Blend);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CullingResults cullingResults = renderingData.cullResults;
            DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("PerPixelLinkedList"),
                ref renderingData, SortingCriteria.CommonTransparent);

            RenderStateBlock renderStateBlock = new RenderStateBlock();
            renderStateBlock.mask = RenderStateMask.Raster;
            renderStateBlock.rasterState = new RasterState(cullMode);

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            CommandBuffer drawPPLLBuffer = CommandBufferPool.Get("PPLL Draw");

            using (new ProfilingScope(drawPPLLBuffer, new ProfilingSampler("DrawPPLL")))
            {
                context.ExecuteCommandBuffer(drawPPLLBuffer);
                drawPPLLBuffer.Clear();
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            }

            context.ExecuteCommandBuffer(drawPPLLBuffer);
            drawPPLLBuffer.Clear();
            drawPPLLBuffer.Dispose();

            CommandBuffer blitBuffer = CommandBufferPool.Get("PPLL Blit Color");

            using (new ProfilingScope(blitBuffer, new ProfilingSampler("PPLLBlitColor")))
            {
                blitBuffer.ClearRandomWriteTargets();
                linkedListMaterial.SetBuffer(fragLinkedBufferID, fragLinkedBuffer);
                linkedListMaterial.SetBuffer(startOffsetBufferID, startOffsetBuffer);
                context.ExecuteCommandBuffer(blitBuffer);
                blitBuffer.Clear();
                Blitter.BlitCameraTexture(blitBuffer, sourceColor, blitRT);
                Blitter.BlitCameraTexture(blitBuffer, sourceColor, sourceColor, linkedListMaterial, 0);
            }

            context.ExecuteCommandBuffer(blitBuffer);
            blitBuffer.Clear();
            blitBuffer.Dispose();
        }
        
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                startOffsetBuffer?.Dispose();
                fragLinkedBuffer?.Dispose();
                blitRT?.Release();
                RenderPipelineManager.beginContextRendering -= RenderCSBuffer;
                CoreUtils.Destroy(linkedListMaterial);
            }
        }
    }
}