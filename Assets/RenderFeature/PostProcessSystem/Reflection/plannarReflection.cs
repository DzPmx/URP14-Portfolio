using System;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;


namespace UnityEngine.Rendering.Universal {
    [ExecuteAlways]
    public class plannarReflectionTest : MonoBehaviour {
        [Serializable]
        public class PlanarReflectionSettings {
            public float m_ClipPlaneOffset = 0.07f;
            public LayerMask m_ReflectLayers = -1;
            public bool m_Shadows;
        }
        
        [SerializeField]
        public PlanarReflectionSettings m_settings = new PlanarReflectionSettings();
        public GameObject targetPlane;
        public float m_planeOffset;
        private Camera SourceCamera;
        private static Camera _reflectionCamera;
        private RenderTexture _reflectionTexture;
        private readonly int _planarReflectionTextureId = Shader.PropertyToID("_ReflectTex");

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += runPlannarReflection;
        }

        private void OnDisable() {
            Cleanup();
        }

        private void OnDestroy() {
            Cleanup();
        }

        private void Cleanup() {
            RenderPipelineManager.beginCameraRendering -= runPlannarReflection;
            if(_reflectionCamera) {  // 
                _reflectionCamera.targetTexture = null;
                SafeDestroy(_reflectionCamera.gameObject);
            }
            if (_reflectionTexture) {  
                RenderTexture.ReleaseTemporary(_reflectionTexture);
            }
        }
        private static void SafeDestroy(Object obj) {
            if (Application.isEditor) {
                DestroyImmediate(obj); 
            }
            else {
                Destroy(obj); 
            }
        }
    
        
        
        private void runPlannarReflection(ScriptableRenderContext context,Camera camera) {
            
            if (targetPlane == null) {
                targetPlane = gameObject;
            }
            if (_reflectionCamera == null) {
                _reflectionCamera = CreateReflectCamera();
            }
            
            var data = new PlanarReflectionSettingData(); 
            data.Set(); // set quality settings
            UpdateReflectionCamera(camera);  
            CreatePlanarReflectionTexture(camera); 
            #pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCamera); // render planar reflections  开始渲染函数
            #pragma warning restore CS0618
            data.Restore(); // restore the quality settings
            Shader.SetGlobalTexture(_planarReflectionTextureId, _reflectionTexture); // Assign texture to water shader
        }

        private int2 ReflectionResolution(Camera cam, float scale) {
            var x = (int)(cam.pixelWidth * scale  );
            var y = (int)(cam.pixelHeight * scale );
            return new int2(x, y);
        }
        

        private void CreatePlanarReflectionTexture(Camera cam) {
            if (_reflectionTexture == null) {
                var res = ReflectionResolution(cam, UniversalRenderPipeline.asset.renderScale);  // 获取 RT 的大小
                const bool useHdr10 = true;
                const RenderTextureFormat hdrFormat = useHdr10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
                _reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 16,
                    GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
            }
            _reflectionCamera.targetTexture =  _reflectionTexture; // 将 RT 赋予相机
        }
        private void UpdateCamera(Camera src, Camera dest) {
            if (dest == null) return;
            dest.aspect = src.aspect;
            dest.cameraType = src.cameraType;   // 这个参数不同步就错
            dest.clearFlags = src.clearFlags;
            dest.fieldOfView = src.fieldOfView;
            dest.depth = src.depth;
            dest.farClipPlane = src.farClipPlane;
            dest.focalLength = src.focalLength;
            dest.useOcclusionCulling = false;
            if (dest.gameObject.TryGetComponent(out UniversalAdditionalCameraData camData)) {  
                camData.renderShadows = m_settings.m_Shadows; // turn off shadows for the reflection camera
            }
        }
        
        private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 reflectionMat = Matrix4x4.identity;
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }
       
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
            var offsetPos = pos + normal * m_settings.m_ClipPlaneOffset;
            var m = cam.worldToCameraMatrix;
            var cameraPosition = m.MultiplyPoint(offsetPos);
            var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }
        private void UpdateReflectionCamera(Camera curCamera) {
            if (targetPlane == null) {
                Debug.LogError("target plane is null!");
            }

            Vector3 planeNormal = targetPlane.transform.up;
            Vector3 planePos = targetPlane.transform.position + planeNormal * m_planeOffset;

            UpdateCamera(curCamera, _reflectionCamera);  // 同步当前相机数据

            // 获取视空间平面，使用反射矩阵，将图像根据平面对称上下颠倒
            var planVS = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector3.Dot(planeNormal, planePos));
            Matrix4x4 reflectionMat = CalculateReflectionMatrix(planVS);
            _reflectionCamera.worldToCameraMatrix = curCamera.worldToCameraMatrix * reflectionMat;
            // 斜截视锥体
            var clipPlane = CameraSpacePlane(_reflectionCamera, planePos, planeNormal, 1.0f);
            var newProjectionMat = CalculateObliqueMatrix(curCamera, clipPlane);
            _reflectionCamera.projectionMatrix = newProjectionMat;
            _reflectionCamera.cullingMask = m_settings.m_ReflectLayers; // never render water layer

        }
        
        private Matrix4x4 CalculateObliqueMatrix(Camera cam, Vector4 plane) {
             var new_M = cam.CalculateObliqueMatrix(plane);
             return new_M;
        }

        private Camera CreateReflectCamera() {
            var go = new GameObject(gameObject.name + " Planar Reflection Camera",typeof(Camera));
            var cameraData = go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            cameraData.renderShadows = false;
            cameraData.SetRenderer(0);  

            var t = transform;
            var reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.transform.SetPositionAndRotation(transform.position, t.rotation);  // 相机初始位置设为当前 gameobject 位置
            reflectionCamera.depth = -10;  // 渲染优先级 [-100, 100]
            reflectionCamera.enabled = false;
            reflectionCamera.tag = "Reflection";
            go.hideFlags = HideFlags.DontSave;

            return reflectionCamera;
        }
        
        class PlanarReflectionSettingData {
            private readonly bool _fog;
            private readonly int _maxLod;
            private readonly float _lodBias;
            private bool _invertCulling;

            public PlanarReflectionSettingData() {
                _fog = RenderSettings.fog;
                _maxLod = QualitySettings.maximumLODLevel;
                _lodBias = QualitySettings.lodBias;
            }

            public void Set() {
                _invertCulling = GL.invertCulling;
                GL.invertCulling = !_invertCulling;  // 因为镜像后绕序会反，将剔除反向
                RenderSettings.fog = false; // disable fog for now as it's incorrect with projection
                QualitySettings.maximumLODLevel = 1;
                QualitySettings.lodBias = _lodBias * 0.5f;
            }

            public void Restore() {
                GL.invertCulling = _invertCulling;
                RenderSettings.fog = _fog;
                QualitySettings.maximumLODLevel = _maxLod;
                QualitySettings.lodBias = _lodBias;
            }
        }
    }
}
