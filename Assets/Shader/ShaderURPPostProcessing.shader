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
        #include "MyPostStack.hlsl"
        ENDHLSL
        Pass
        {
            name"ColorTint"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment ColorTintFragment
            ENDHLSL
        }

        Pass
        {
            name"GussianBlurHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment GaussianBlurHorizontalPassFragment
            ENDHLSL
        }
        
        Pass
        {
            name"GussianBlurVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment GaussianBlurVerticalPassFragment
            ENDHLSL
        }
        
        Pass
        {
            name"BoxBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment BoxBlurFragment
            ENDHLSL
        }
        
        Pass
        {
            name"KawaseBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment KawaseBlurFragment
            ENDHLSL
        }

    }
}