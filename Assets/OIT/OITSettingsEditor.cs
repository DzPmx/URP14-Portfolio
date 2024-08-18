using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OITSettings))]
public class OITSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        OITSettings oitSettings = (OITSettings)target;
        oitSettings.oitMode = (OITMode)EditorGUILayout.EnumPopup("OIT Mode", oitSettings.oitMode);
        switch (oitSettings.oitMode)
        {
            case OITMode.DepthingPeeling:
                oitSettings.layers = (int)EditorGUILayout.Slider("Depth Peeling Layers", oitSettings.layers, 1, 6);
                oitSettings.initialShader = (Shader)
                    EditorGUILayout.ObjectField("Initial Shader", oitSettings.initialShader, typeof(Shader), false);
                oitSettings.depthPeelingShader = (Shader)
                    EditorGUILayout.ObjectField("Depth Peeling Shader", oitSettings.depthPeelingShader, typeof(Shader),
                        false);
                oitSettings.blendShader = (Shader)
                    EditorGUILayout.ObjectField("Blend Shader", oitSettings.blendShader, typeof(Shader), false);
                break;
            case OITMode.WeightBlend:
                break;
            case OITMode.PerpiexlLinkList:
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(oitSettings);
        }

        serializedObject.ApplyModifiedProperties();
    }
}