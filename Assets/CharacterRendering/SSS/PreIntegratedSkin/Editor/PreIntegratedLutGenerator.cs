using UnityEditor;
using UnityEngine;

namespace PreIntegratedSkin.Editor
{
    public class PreIntegratedLutGenerator : EditorWindow
    {
        [MenuItem("Tools/SkinLutGenerator", false, 1)]
        private static void LutGeneratorWindow()
        {
            GetWindow<PreIntegratedLutGenerator>("Pre-Integrated LUT Generator");
        }

        private RenderTexture lut;
        private Texture2D savedTexture;
        private ComputeShader lutCompute;
        private IntegralInterval interval = IntegralInterval.Half;
        private LutSize lutSize = LutSize._1024x1024;
        private TextureType textureType = TextureType.SkinDiffsue;
        private static int lutID = Shader.PropertyToID("_LUT");
        private static int lutSizeID = Shader.PropertyToID("_LutSize");
        private static int integralIntervalID = Shader.PropertyToID("_IntegralInterval");
        private string savedPath;

        private void OnEnable()
        {
            lutCompute =
                AssetDatabase.LoadAssetAtPath<ComputeShader>(
                    "Assets/CharacterRendering/SSS/PreIntegratedSkin/Shader/PreIntegratedLutCompute.compute");
            if (lutCompute == null)
            {
                Debug.Log("Do Not Find LUT ComputeShader ");
            }
        }

        private void OnGUI()
        {
            ShowSettings();
            BakeAndSaveLut();
            PreviewTexture();
        }

        private void OnDisable()
        {
            if (lut)
            {
                RenderTexture.ReleaseTemporary(lut);
            }
        }

        private void OnDestroy()
        {
            if (lut)
            {
                RenderTexture.ReleaseTemporary(lut);
            }
        }

        public enum IntegralInterval
        {
            Half = 1,
            Full = 2,
        }

        public enum TextureType
        {
            SkinDiffsue,
            SkinDiffuseSpecialEdition,
            SkinDiffuseNvidiaFaceWorks,
            SkinShadowNvidiaFaceWorks,
            SkinSpecular,
        }

        public enum LutSize
        {
            _256x256 = 256,
            _512x512 = 512,
            _1024x1024 = 1024,
            _2048x2048 = 2048,
        }

        private void PreviewTexture()
        {
            var rect = EditorGUILayout.GetControlRect(true, position.height);
            if (lut != null)
            {
                EditorGUI.DrawPreviewTexture(rect, lut);
            }
            else
            {
                EditorGUI.DrawRect(rect, Color.black);
            }
        }

        private void ShowSettings()
        {
            EditorGUILayout.LabelField("参数设置:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            textureType = (TextureType)EditorGUILayout.EnumPopup("纹理类型", textureType);
            if (textureType == TextureType.SkinDiffsue ||
                textureType == TextureType.SkinDiffuseSpecialEdition)
            {
                interval = (IntegralInterval)EditorGUILayout.EnumPopup("积分区间", interval);
            }

            lutSize = (LutSize)EditorGUILayout.EnumPopup("纹理大小", lutSize);
            if (EditorGUI.EndChangeCheck() && lut != null)
            {
                RenderTexture.ReleaseTemporary(lut);
            }

            EditorGUILayout.EndVertical();
        }

        private void BakeAndSaveLut()
        {
            EditorGUILayout.LabelField("预览与存储:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("(注)为了解决纹理Banding的问题,预览图会比存储的图片颜色浅,这是正常的");
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (GUILayout.Button("烘培纹理"))
            {
                SetupLut();
                switch (textureType)
                {
                    case TextureType.SkinDiffsue:
                        BakeDiffuse();
                        break;
                    case TextureType.SkinDiffuseSpecialEdition:
                        BakeDiffuseSpecialEdition();
                        break;
                    case TextureType.SkinDiffuseNvidiaFaceWorks:
                        BakeDiffuseNvidiaFaceWorks();
                        break;
                    case TextureType.SkinShadowNvidiaFaceWorks:
                        BakeShadowNvidiaFaceWorks();
                        break;
                    case TextureType.SkinSpecular:
                        BakeSpecular();
                        break;
                }
            }

            if (GUILayout.Button("存储纹理"))
            {
                if (lut == null)
                {
                    Debug.Log("请先烘焙纹理，再存储");
                    return;
                }

                SetupTexture2D();
                SaveLut();
                ReImport();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SetupLut()
        {
            lut = RenderTexture.GetTemporary((int)lutSize, (int)lutSize, 0, RenderTextureFormat.ARGB64);
            lut.enableRandomWrite = true;
        }

        private void SetupTexture2D()
        {
            savedTexture = new Texture2D((int)lutSize, (int)lutSize, TextureFormat.RGBA64, true, true);
            savedTexture.name = textureType.ToString() + "Lut.TGA";
        }

        private void BakeDiffuse()
        {
            int kernelIndex = lutCompute.FindKernel("CSDiffuse");
            lutCompute.SetTexture(kernelIndex, lutID, lut);
            lutCompute.SetFloat(integralIntervalID, (float)interval);
            lutCompute.SetFloat(lutSizeID, (float)lutSize);
            lutCompute.Dispatch(kernelIndex, 512, 512, 1);
        }

        private void BakeDiffuseSpecialEdition()
        {
            int kernelIndex = lutCompute.FindKernel("CSDiffuseSpecial");
            lutCompute.SetTexture(kernelIndex, lutID, lut);
            lutCompute.SetFloat(integralIntervalID, (float)interval);
            lutCompute.SetFloat(lutSizeID, (float)lutSize);
            lutCompute.Dispatch(kernelIndex, 512, 512, 1);
        }

        private void BakeDiffuseNvidiaFaceWorks()
        {
            int kernelIndex = lutCompute.FindKernel("CSDiffuseNvidiaFaceWorks");
            lutCompute.SetTexture(kernelIndex, lutID, lut);
            lutCompute.SetFloat(integralIntervalID, (float)interval);
            lutCompute.SetFloat(lutSizeID, (float)lutSize);
            lutCompute.Dispatch(kernelIndex, 512, 512, 1);
        }

        private void BakeShadowNvidiaFaceWorks()
        {
            int kernelIndex = lutCompute.FindKernel("CSShadowNvidiaFaceWorks");
            lutCompute.SetTexture(kernelIndex, lutID, lut);
            lutCompute.SetFloat(integralIntervalID, (float)interval);
            lutCompute.SetFloat(lutSizeID, (float)lutSize);
            lutCompute.Dispatch(kernelIndex, 512, 512, 1);
        }

        private void BakeSpecular()
        {
            int kernelIndex = lutCompute.FindKernel("CSSpecular");
            lutCompute.SetTexture(kernelIndex, lutID, lut);
            lutCompute.SetFloat(lutSizeID, (float)lutSize);
            lutCompute.Dispatch(kernelIndex, 512, 512, 1);
        }

        private void SaveLut()
        {
            if (lut)
            {
                RenderTexture.active = lut;
                savedTexture.ReadPixels(new Rect(0, 0, lut.width, lut.height), 0, 0);
                savedTexture.Apply();
                RenderTexture.active = null;
                savedPath = "Assets/CharacterRendering/SSS/PreIntegratedSkin/Resources/" + savedTexture.name;
                System.IO.File.WriteAllBytes(savedPath, savedTexture.EncodeToTGA());
                AssetDatabase.ImportAsset(savedPath);
            }
        }

        private void ReImport()
        {
            var importer = AssetImporter.GetAtPath(savedPath) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = true;
                importer.maxTextureSize = (int)lutSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
    }
}