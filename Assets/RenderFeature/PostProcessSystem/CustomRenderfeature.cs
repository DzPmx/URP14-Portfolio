using System.Collections.Generic;
using System.Linq;
using RenderFeature.RenderPass;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature
{
    public class CustomRenderfeature : ScriptableRendererFeature
    {
        private List<MyPostProcessing> myPostProcessings;
        private CustomPostProcessingPass mAfterOpaqueAndSkyPass;
        private CustomPostProcessingPass mBeforePostProcessPass;
        private CustomPostProcessingPass mAfterPostProcessPass;

        public override void Create()
        {
            var stack = VolumeManager.instance.stack;
            myPostProcessings = VolumeManager.instance.baseComponentTypeArray
                .Where(t => t.IsSubclassOf(typeof(MyPostProcessing)))
                .Select(t => stack.GetComponent(t) as MyPostProcessing).ToList();

            GetRenderPass();
        }

        public void GetRenderPass()
        {
            var afterOpaqueAndSkyCPPs = myPostProcessings
                .Where(c => c.injectPoint == CustomPostProcessInjectPoint.AfterOpaqueAndSky)
                .OrderBy(c => c.OrderInInjectionPoint).ToList();
            mAfterOpaqueAndSkyPass = new CustomPostProcessingPass();
            mAfterOpaqueAndSkyPass.Setup("Custom PostProcess after Skybox", afterOpaqueAndSkyCPPs);
            mAfterOpaqueAndSkyPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            var beforePostProcessingCPPs = myPostProcessings
                .Where(c => c.injectPoint == CustomPostProcessInjectPoint.BeforePostProcess)
                .OrderBy(c => c.OrderInInjectionPoint).ToList();
            mBeforePostProcessPass = new CustomPostProcessingPass();
            mBeforePostProcessPass.Setup("Custom PostProcess before PostProcess", beforePostProcessingCPPs);
            mBeforePostProcessPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            var afterPostProcessCPPs = myPostProcessings
                .Where(c => c.injectPoint == CustomPostProcessInjectPoint.AfterPostProcess)
                .OrderBy(c => c.OrderInInjectionPoint).ToList();
            mAfterPostProcessPass = new CustomPostProcessingPass();
            mAfterPostProcessPass.Setup("Custom PostProcess after PostProcess", afterPostProcessCPPs);
            mAfterPostProcessPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled) return;
            if (mAfterOpaqueAndSkyPass.SetupCustomPostProcessing())
            {
                mAfterOpaqueAndSkyPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(mAfterOpaqueAndSkyPass);
            }

            if (mBeforePostProcessPass.SetupCustomPostProcessing())
            {
                mBeforePostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(mBeforePostProcessPass);
            }

            if (mAfterPostProcessPass.SetupCustomPostProcessing())
            {
                mAfterPostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(mAfterPostProcessPass);
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mAfterOpaqueAndSkyPass?.Dispose();
            mBeforePostProcessPass?.Dispose();
            mAfterPostProcessPass?.Dispose();
            if (disposing && myPostProcessings != null)
            {
                foreach (var item in myPostProcessings)
                {
                    item.Dispose();
                }
            }
        }
    }
}