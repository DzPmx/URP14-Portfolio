Shader "MyURPShader/ReflectionTest"
{
    SubShader
    {
        LOD 100
        Tags
        {
            "RenderPipeLine"="UniversalPipeLine"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        ENDHLSL
        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            CBUFFER_START (UnityPerMaterial)
            CBUFFER_END
            TEXTURE2D(_ReflectTex);
            SAMPLER (sampler_ReflectTex);
            

            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct Varings
            {
                float4 posCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            Varings vert(Attributes input)
            {
                Varings output;
                output.posCS = TransformObjectToHClip(input.posOS.xyz);
                output.uv0 = input.uv0;
                return output;
            }

            float4 frag(Varings input) : SV_Target
            {
                float2 screenUV=input.posCS.xy/_ScaledScreenParams.xy;
                return SAMPLE_TEXTURE2D(_ReflectTex, sampler_ReflectTex,screenUV);
            }
            ENDHLSL
        }
    }
}