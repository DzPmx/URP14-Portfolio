using OIT;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OIT
{
    public class OITRenderfeature : ScriptableRendererFeature
    {
        public OITSettings oitSettings;
        private DepthPeelingOITRenderPass _depthPeelingOitRenderPass;
        private WeightedBlendOITRenderPass _weightedBlendOitRenderPass;
        private PerPixelLinkedListRenderPass _perPixelLinkedListRenderPass;

        public override void Create()
        {
            name = "OIT";

            switch (oitSettings.oitMode)
            {
                case OITMode.DepthPeeling:
                    _depthPeelingOitRenderPass?.Dispose(true);
                    _depthPeelingOitRenderPass = new DepthPeelingOITRenderPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };
                    break;
                case OITMode.WeightedBlend:
                    _weightedBlendOitRenderPass?.Dispose(true);
                    _weightedBlendOitRenderPass = new WeightedBlendOITRenderPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };
                    break;
                case OITMode.PerpiexlLinkedList:
                    _perPixelLinkedListRenderPass?.Dispose(true);
                    _perPixelLinkedListRenderPass = new PerPixelLinkedListRenderPass()
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };
                    break;
                case OITMode.PreviewAll:
                    _depthPeelingOitRenderPass?.Dispose(true);
                    _weightedBlendOitRenderPass?.Dispose(true);
                    _perPixelLinkedListRenderPass?.Dispose(true);
                    _depthPeelingOitRenderPass = new DepthPeelingOITRenderPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };

                    _weightedBlendOitRenderPass = new WeightedBlendOITRenderPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };

                    _perPixelLinkedListRenderPass = new PerPixelLinkedListRenderPass()
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                    };
                    break;
            }
        }

        public override bool SupportsNativeRenderPass()
        {
            return true;
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            switch (oitSettings.oitMode)
            {
                case OITMode.DepthPeeling:
                    _depthPeelingOitRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    break;
                case OITMode.WeightedBlend:
                    _weightedBlendOitRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    break;
                case OITMode.PerpiexlLinkedList:
                    _perPixelLinkedListRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    break;
                case OITMode.PreviewAll:
                    _depthPeelingOitRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    _weightedBlendOitRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    _perPixelLinkedListRenderPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
                        renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
                    break;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Dont Consider ReflecitionProbe Yet 
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            switch (oitSettings.oitMode)
            {
                case OITMode.DepthPeeling:
                    renderer.EnqueuePass(_depthPeelingOitRenderPass);
                    break;
                case OITMode.WeightedBlend:
                    renderer.EnqueuePass(_weightedBlendOitRenderPass);
                    break;
                case OITMode.PerpiexlLinkedList:
                    renderer.EnqueuePass(_perPixelLinkedListRenderPass);
                    break;
                case OITMode.PreviewAll:
                    renderer.EnqueuePass(_depthPeelingOitRenderPass);
                    renderer.EnqueuePass(_weightedBlendOitRenderPass);
                    renderer.EnqueuePass(_perPixelLinkedListRenderPass);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            switch (oitSettings.oitMode)
            {
                case OITMode.DepthPeeling:
                    _depthPeelingOitRenderPass?.Dispose(disposing);
                    break;
                case OITMode.WeightedBlend:
                    _weightedBlendOitRenderPass?.Dispose(disposing);
                    break;
                case OITMode.PerpiexlLinkedList:
                    _perPixelLinkedListRenderPass?.Dispose(disposing);
                    break;
                case OITMode.PreviewAll:
                    _depthPeelingOitRenderPass?.Dispose(disposing);
                    _weightedBlendOitRenderPass?.Dispose(disposing);
                    _perPixelLinkedListRenderPass?.Dispose(disposing);
                    break;
            }
        }
    }
}