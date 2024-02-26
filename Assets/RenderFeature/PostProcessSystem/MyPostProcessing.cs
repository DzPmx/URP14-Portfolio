using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public abstract class MyPostProcessing : VolumeComponent, IPostProcessComponent, IDisposable
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

public enum PostStackPass
{
    ColorTint,
    GussianBlurHorizontal,
    GussianBlurVertical,
    BoxBlur,
    KawaseBlur,
    DualBlur,
}

public enum CustomPostProcessInjectPoint
{
    AfterOpaqueAndSky,
    BeforePostProcess,
    AfterPostProcess,
}