Shader "MyURPShader/ShaderURPPostProcessing"
{
    SubShader
    {
        LOD 100
        Tags
        {
            "RenderPipeLine"="UniversalPipeLine"
            "RenderType"="Opaque"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities//blit.hlsl"
        ENDHLSL
        Pass
        {
            name"DrawProcedure"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_GrabTexture);
            SAMPLER(sampler_GrabTexture);
            float4 _ColorTint;

            #if SHADER_API_GLES
                struct Attributes
                {
                    float4 posOS       : POSITION;
                    float2 uv0          : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
            #else
            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            #endif

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                #if SHADER_API_GLES
                output.posCS = input.posOS;
                output.uv0 = input.uv0;
                #else
                output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);
                #endif

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_GrabTexture, sampler_GrabTexture, input.uv0) * float4(
                    _ColorTint.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}