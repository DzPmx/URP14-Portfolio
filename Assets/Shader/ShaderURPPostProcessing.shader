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
            #pragma fragment colorTintFrag
            
            ENDHLSL
        }

        Pass
        {
            name"BlurHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment BlurHorizontalPassFragment
            ENDHLSL
        }
        
        Pass
        {
            name"BlurVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment BlurVerticalPassFragment
            ENDHLSL
        }
    }
}