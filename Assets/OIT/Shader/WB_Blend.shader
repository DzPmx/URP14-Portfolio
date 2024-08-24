Shader "Universal Render Pipeline/OIT/WB_Blend"
{
    SubShader
    {

        Pass
        {
            ZTest Always
            Cull Back
            ZWrite Off

            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_AccumulateRT);
            SAMPLER(sampler_AccumulateRT);
            TEXTURE2D(_RevealageRT);
            SAMPLER(sampler_RevealageRT);
            TEXTURE2D(_BlitRT);
            SAMPLER(sampler_BlitRT);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                half4 blit = SAMPLE_TEXTURE2D(_BlitRT, sampler_BlitRT, input.uv0);
                float4 accum = SAMPLE_TEXTURE2D(_AccumulateRT, sampler_AccumulateRT, input.uv0);
                float reve = SAMPLE_TEXTURE2D(_RevealageRT, sampler_RevealageRT, input.uv0).r;
                float4 col = float4(accum.rgb / clamp(accum.a, 1e-4, 5e4), reve);
                return (1.0 - col.a) * col + col.a * blit;
            }
            ENDHLSL
        }

    }
}