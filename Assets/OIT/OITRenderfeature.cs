using System;
using OIT.DepthPeeling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OITRenderfeature : ScriptableRendererFeature
{
    public OITSettings oitSettings;
    private DepthPeelingRenderPass _oitRenderTransparentPass;

    public override void Create()
    {
        _oitRenderTransparentPass = new DepthPeelingRenderPass();
        name = "OIT";
        _oitRenderTransparentPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override bool SupportsNativeRenderPass()
    {
        return true;
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _oitRenderTransparentPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle,
            renderingData.cameraData.renderer.cameraDepthTargetHandle, oitSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //Dont Consider ReflecitionProbe Yet 
        if (renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }
        
        renderer.EnqueuePass(_oitRenderTransparentPass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _oitRenderTransparentPass.Dispose();
    }
}