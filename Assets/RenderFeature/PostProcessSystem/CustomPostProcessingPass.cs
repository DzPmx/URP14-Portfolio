using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderFeature.RenderPass
{
    public class CustomPostProcessingPass : ScriptableRenderPass
    {
        private List<MyPostProcessing> myPostProcessings;
        private List<int> mActiveCustomPostProcessingIndex;
        private string mProfilerTag;
        private List<ProfilingSampler> mProfilingSamplers;

        private RTHandle mSourceRT;
        private RTHandle mDestRT;
        private RTHandle mTempRT0;
        private RTHandle mTempRT1;

        private const string mTempRT0Name = "_TemporaryRenderTexture0";
        private const string mTempRT1Name = "_TemporaryRenderTexture1";

        public void Setup(string profilerTag, List<MyPostProcessing> myPostProcessings)
        {
            mProfilerTag = profilerTag;
            this.myPostProcessings = myPostProcessings;
            mActiveCustomPostProcessingIndex = new List<int>(myPostProcessings.Count);
            mProfilingSamplers = this.myPostProcessings.Select(c => new ProfilingSampler(c.ToString())).ToList();
        }

        public bool SetupCustomPostProcessing()
        {
            mActiveCustomPostProcessingIndex.Clear();
            for (int i = 0; i < myPostProcessings.Count; i++)
            {
                myPostProcessings[i].Setup();
                if (myPostProcessings[i].IsActive())
                {
                    mActiveCustomPostProcessingIndex.Add(i);
                }
            }

            return mActiveCustomPostProcessingIndex.Count != 0;
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref mTempRT0, descriptor, name: mTempRT0Name);
            //bool rt1Used = false;
            if (mActiveCustomPostProcessingIndex.Count > 1)
            {
                RenderingUtils.ReAllocateIfNeeded(ref mTempRT1, descriptor, name: mTempRT1Name);
                // rt1Used = true;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(mProfilerTag);

            mDestRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
            mSourceRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (mActiveCustomPostProcessingIndex.Count==0)return;
            if (mActiveCustomPostProcessingIndex.Count == 1)
            {
                int index = mActiveCustomPostProcessingIndex[0];
                using (new ProfilingScope(cmd, mProfilingSamplers[index]))
                {
                    myPostProcessings[index].OnCameraSetup(cmd, ref renderingData);
                    myPostProcessings[index].Render(cmd, ref renderingData, mSourceRT, mTempRT0);
                }
            }
            else
            {
                using (new ProfilingScope(cmd,new ProfilingSampler("Grab Pass")))
                {
                    Blitter.BlitCameraTexture(cmd, mSourceRT, mTempRT0);
                }
                for (int i = 0; i < mActiveCustomPostProcessingIndex.Count; i++)
                {
                    int index = mActiveCustomPostProcessingIndex[i];
                    var myPostProcessing = myPostProcessings[index];
                    using (new ProfilingScope(cmd, mProfilingSamplers[index]))
                    {
                        myPostProcessing.OnCameraSetup(cmd, ref renderingData);
                        myPostProcessing.Render(cmd, ref renderingData, mTempRT0, mTempRT1);
                    }

                    CoreUtils.Swap(ref mTempRT0, ref mTempRT1);
                }
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("Back To Camera")))
            {
                Blitter.BlitCameraTexture(cmd, mTempRT0, mDestRT);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.Dispose();
        }

        public void Dispose()
        {
            mTempRT0?.Release();
            mTempRT1?.Release();
        }
    }
}