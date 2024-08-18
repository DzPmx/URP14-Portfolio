using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[ExecuteAlways]
[CreateAssetMenu(fileName = "OITSettings", menuName = "OIT Settings")]
public class OITSettings : ScriptableObject
{
    public OITMode oitMode = OITMode.DepthingPeeling;

    //Depth Peeling
    public int layers = 2;
    public Shader initialShader;
    public Shader depthPeelingShader;
    public Shader blendShader;

    private void OnEnable()
    {
        initialShader = Shader.Find("Universal Render Pipeline/OIT/DP_Initial");
        depthPeelingShader = Shader.Find("Universal Render Pipeline/OIT/DP_DepthPeeling");
        blendShader = Shader.Find("Universal Render Pipeline/OIT/DP_Blend");
    }

    //WeightBlend

    //PerpixelLinkList
}

public enum OITMode
{
    DepthingPeeling = 1,
    WeightBlend = 2,
    PerpiexlLinkList = 3,
}