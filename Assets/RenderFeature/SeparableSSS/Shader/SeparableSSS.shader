Shader "MyURPShader/Character Renderding/SeparableSSS"
{
    SubShader
    {
        Tags
        {
            "RenderPipeLine"="UniversalPipeLine"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        Stencil
        {
            Ref 5
            comp equal
            Pass Replace
        }
        ZTest Always
        ZWrite Off
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ShaderLibrary/SeparableSubsurfaceScatter.hlsl"
        CBUFFER_START(UnityPerMaterial)
        CBUFFER_END
        ENDHLSL
        Pass
        {
            name"SSSS XBlur"
            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment XBlurFrag
            #pragma target 3.5

            float4 XBlurFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 SceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
                float SSSIntencity = (_SSSScale * _CameraDepthTexture_TexelSize.x);
                float3 XBlurPlus = SeparableSubsurface(SceneColor, input.uv0, float2(SSSIntencity, 0)).rgb;
                float3 XBlurNagteiv = SeparableSubsurface(SceneColor, input.uv0, float2(-SSSIntencity, 0)).rgb;
                float3 XBlur = (XBlurPlus + XBlurNagteiv) / 2;
                return float4(saturate(XBlur), SceneColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            name"SSSS YBlur"
            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment YBlurFrag
            #pragma target 3.5

            float4 YBlurFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 SceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
                float SSSIntencity = (_SSSScale * _CameraDepthTexture_TexelSize.y);
                float3 YBlurPlus = SeparableSubsurface(SceneColor, input.uv0, float2(0, SSSIntencity)).rgb;
                float3 YBlurNagteiv = SeparableSubsurface(SceneColor, input.uv0, float2(0, -SSSIntencity)).rgb;
                float3 YBlur = (YBlurPlus + YBlurNagteiv) / 2;
                return float4(saturate(YBlur), SceneColor.a);
            }
            ENDHLSL
        }
    }
}