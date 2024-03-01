using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class RenderQuestTest : MonoBehaviour
{
    private RenderTexture rt;
    private new Camera camera;
    private RenderTextureDescriptor descriptor;

    private void Awake()
    {
        camera = GetComponent<Camera>();
        descriptor = new RenderTextureDescriptor(Screen.width / 2, Screen.height / 2);
    }

    private void OnEnable()
    {
        UseRenderRequest();
        //RenderPipelineManager.beginCameraRendering += useRenderSingleCamera;
    }

    void Update()
    {
        UseRenderRequest();
        Shader.SetGlobalTexture("_ReflectTex", rt);
    }

    // void useRenderSingleCamera(ScriptableRenderContext context, Camera camera)
    // {
    //     this.camera = GetComponent<Camera>();
    //     if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
    //     {
    //         return;
    //     }
    //
    //     if (rt is null)
    //     {
    //         rt = RenderTexture.GetTemporary(descriptor);
    //         this.camera.targetTexture = rt;
    //     }
    //
    //     UniversalRenderPipeline.RenderSingleCamera(context, this.camera);
    // }

    void UseRenderRequest()
    {
        if (rt is null)
        {
            rt = RenderTexture.GetTemporary(descriptor);
        }

        // RenderTexture.active = rt;
        // GL.Clear(true, true, Color.clear);
        // RenderTexture.active = null;
        UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        if (RenderPipeline.SupportsRenderRequest(camera, request))
        {
            if (rt != null)
            {
                request.destination = rt;
                UniversalRenderPipeline.SubmitRenderRequest(camera, request);
            }
        }
    }

    private void OnDisable()
    {
        //RenderPipelineManager.beginCameraRendering -= useRenderSingleCamera;
        if (rt)
        {
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private void OnDestroy()
    {
        //RenderPipelineManager.beginCameraRendering -= useRenderSingleCamera;
        if (rt)
        {
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}