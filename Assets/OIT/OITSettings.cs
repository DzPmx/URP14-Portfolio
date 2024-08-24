using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OIT
{
    [Serializable]
    [CreateAssetMenu(fileName = "OITSettings", menuName = "OIT Settings")]
    public class OITSettings : ScriptableObject
    {
        public OITMode oitMode = OITMode.DepthPeeling;

        //Depth Peeling
        [SerializeField]public int layers = 2;
        [SerializeField]public Shader DP_initialShader;
        [SerializeField]public Shader DP_depthPeelingShader;
        [SerializeField]public Shader DP_blendShader;

        //WeightedBlend
        [SerializeField]public WeightFunction WeightFunction = WeightFunction.Function1;
        [SerializeField]public Shader WB_accumulateShader;
        [SerializeField]public Shader WB_revealageShader;
        [SerializeField]public Shader WB_blendShader;

        private void OnEnable()
        {
            DP_initialShader = Shader.Find("Universal Render Pipeline/OIT/DP_Initial");
            DP_depthPeelingShader = Shader.Find("Universal Render Pipeline/OIT/DP_DepthPeeling");
            DP_blendShader = Shader.Find("Universal Render Pipeline/OIT/DP_Blend");

            WB_accumulateShader = Shader.Find("Universal Render Pipeline/OIT/WB_Accumulate");
            WB_revealageShader = Shader.Find("Universal Render Pipeline/OIT/WB_Revealage");
            WB_blendShader = Shader.Find("Universal Render Pipeline/OIT/WB_Blend");

        }


        //PerpixelLinkList
    }

    public enum OITMode
    {
        DepthPeeling = 1,
        WeightedBlend = 2,
        PerpiexlLinkedList = 3,
        PreviewAll = 4,
    }

    public enum WeightFunction
    {
        NoWeighted,
        Function1,
        Function2,
        Function3,
    }
}