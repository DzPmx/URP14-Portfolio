Shader "MyURPShader/Character Rendering/Burley Normalized SSS"
{
    SubShader
    {
        Tags
        {
            "RenderPipeLine"="UniversalPipeLine"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Burley Normalized SSS"
            Stencil
            {
                Ref 10
                comp equal
                Pass Replace
            }
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex   Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShaderLibrary/BurleyNormalizedSSS.hlsl"
            #include "ShaderLibrary/BurleyNormalizedSSSPass.hlsl"
            ENDHLSL
        }
    }
}