using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using File = UnityEngine.Windows.File;

public class CreateURPDefaultShader
{   
    //模板Shader路径
    private const string URP_Default_Shader = "Assets/Script/Editor/URPDefault.shader";
    private const string URP_PostProcessing_Shader = "Assets/Script/Editor/URPPostProcessing.shader";

    [MenuItem("Assets/Create/Shader/URP Shader", false)]
    static void CreateDefaultShader()
    {
        string locationPath = GetSelectedPathOrFallback();
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<MyDoCreateAsset>(), locationPath + "URPDefault.shader", null,
            URP_Default_Shader);
    }
    [MenuItem("Assets/Create/Shader/URP PostProcessingShader", false)]
    static void CreatePostProcessingShader()
    {
        string locationPath = GetSelectedPathOrFallback();
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<MyDoCreateAsset>(), locationPath + "URPPostProcessing.shader", null,
            URP_PostProcessing_Shader);
    }

    public static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                break;
            }
        }
        return path;
    }

    class MyDoCreateAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            Object o = CreateShaderAssetFromTemplate(pathName, resourceFile);
            ProjectWindowUtil.ShowCreatedAsset(o);
        }

        internal static Object CreateShaderAssetFromTemplate(string pathName, string resourceFile)
        {
            string fullPath = Path.GetFullPath(pathName);
            StreamReader streamReader = new StreamReader(resourceFile);
            string text = streamReader.ReadToEnd();
            streamReader.Close();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathName);
            text = Regex.Replace(text, "#Name#", fileNameWithoutExtension);
            bool encoderShouldEmitUTF8Identifier = true;
            bool throwOnInvalidBytes = false;
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes);
            bool append = false;
            StreamWriter streamWriter = new StreamWriter(fullPath, append, encoding);
            streamWriter.Write(text);
            streamWriter.Close();
            AssetDatabase.ImportAsset(pathName);
            return AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));
        }
    }
}