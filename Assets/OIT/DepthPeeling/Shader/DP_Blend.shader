Shader "Universal Render Pipeline/OIT/DP_Blend"
{
    SubShader
    {
        ZTest Always
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_LayerTex);
            SAMPLER(sampler_LayerTex);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
                half4 layer = SAMPLE_TEXTURE2D(_LayerTex, sampler_LayerTex, input.uv0);
                return layer.a * layer + (1 - layer.a) * col;
            }
            ENDHLSL
        }

    }
}