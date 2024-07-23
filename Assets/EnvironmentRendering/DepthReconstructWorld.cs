using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleRendererFeature]
public class DepthReconstructWorld : ScriptableRendererFeature
{
    class DepthReconstructPass : ScriptableRenderPass
    {
        private static int mCameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        private static int mCameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static int mCameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static int mProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();  
            Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix();  
            // 将camera view space 的平移置为0，用来计算world space下相对于相机的vector  
            Matrix4x4 cview = view;  
            cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));  
            Matrix4x4 cviewProj = proj * cview;  

            // 计算viewProj逆矩阵，即从裁剪空间变换到世界空间  
            Matrix4x4 cviewProjInv = cviewProj.inverse;  

            // 计算世界空间下，近平面四个角的坐标  
            var near = renderingData.cameraData.camera.nearClipPlane;  
            Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-near, near, -near, near));  
            Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1.0f, 1.0f, -1.0f, 1.0f));  
            Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f));  

            // 计算相机近平面上方向向量  
            Vector4 cameraXExtent = topRightCorner - topLeftCorner;  
            Vector4 cameraYExtent = bottomLeftCorner - topLeftCorner;  

            near = renderingData.cameraData.camera.nearClipPlane;  

            cmd.SetGlobalVector(mCameraViewTopLeftCornerID, topLeftCorner);  
            cmd.SetGlobalVector(mCameraViewXExtentID, cameraXExtent);  
            cmd.SetGlobalVector(mCameraViewYExtentID, cameraYExtent);  
            cmd.SetGlobalVector(mProjectionParams2ID, new Vector4(1.0f / near, renderingData.cameraData.worldSpaceCameraPos.x, renderingData.cameraData.worldSpaceCameraPos.y, renderingData.cameraData.worldSpaceCameraPos.z));  
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }
    }

    DepthReconstructPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new DepthReconstructPass();
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
    }
}


