using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public abstract class PostProcessSystem : VolumeComponent, IPostProcessComponent, IDisposable
{
    public abstract bool IsActive();
    public virtual bool IsTileCompatible() => false;

    public virtual CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;
    public virtual int OrderInInjectionPoint => 0;
    public abstract void Setup();

    public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    public abstract void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing)
    {
    }
}



public enum BlurPass
{
    GussianBlurHorizontal,
    GussianBlurVertical,
    BoxBlur,
    KawaseBlur,
    DualBlurDown,
    DualBlurUp,
    BokehBlur,
    TiltShiftBlur,
    TiltShiftBokehBlurDebug,
    IrisBlur,
    IrisBlurDebug,
    GrainyBlur,
    RadialBlur,
    DirectionalBlur
}

public enum GlitchPass
{
    RGBSplitHorizontal,
    RGBSplitVertical,
    RGBSplitCombine,
    ImageBlock,
    LineBlockHorizontal,
    LineBlockVertical,
    TileJitterHorizontal,
    TileJitterVertical,
    ScanLineJitterHorizontal,
    ScanLineJitterVertical,
    DigitalStripe,
    AnalogNoise,
    WaveJitterHorizontal,
    WaveJitterVertical,

}

public enum IntervalType
{
    Random,
    Infinite,
}

public enum CustomPostProcessInjectPoint
{
    AfterOpaqueAndSky,
    BeforePostProcess,
    AfterPostProcess,
}