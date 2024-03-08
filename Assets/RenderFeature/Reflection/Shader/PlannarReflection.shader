Shader "MyURPShader/PlaanarReflection"
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
            float _LOD;
            CBUFFER_END
            TEXTURE2D(_PlanarReflectionTexture);
            SAMPLER (sampler_PlanarReflectionTexture);


            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.posCS = TransformObjectToHClip(input.posOS.xyz);
                output.uv0 = input.uv0;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.posCS.xy / _ScaledScreenParams.xy;
                return SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, screenUV);
            }
            ENDHLSL
        }
    }
}