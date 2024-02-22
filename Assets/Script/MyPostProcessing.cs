using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class MyPostProcessing 
{
    public static bool ShouldRender(ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType==CameraType.Game)
        {
            return true;
        }

        return false;
    }
    public void Draw(CommandBuffer buffer, in RTHandle source ,in RTHandle dest)
    {
       
    }
}
