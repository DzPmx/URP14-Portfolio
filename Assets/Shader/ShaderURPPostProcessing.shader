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

        Pass
        {
            name"DualKawaseBlurDownSample"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment DualKawaseBlurDownSampleFragment
            ENDHLSL
        }

        Pass
        {
            name"DualKawaseBlurUpSample"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment DualKawaseBlurUpSampleFragment
            ENDHLSL
        }

        Pass
        {
            name"BokehBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment BokehBlurFragment
            ENDHLSL
        }
        
        Pass
        {
            name"TiltShiftBokehBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment TiltShiftBokehBlurFragment
            ENDHLSL
        }
        
        Pass
        {
            name"TiltShiftBokehBlurDebug"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment TiltShiftBokehBlurDebug
            ENDHLSL
        }
        Pass
        {
            name"IrisBokehBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment IrisBokehBlurFragment
            ENDHLSL
        }
        
        Pass
        {
            name"IrisBokehBlurDebug"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment IrisBokehBlurFragmentDebug
            ENDHLSL
        }

        Pass
        {
            name"GrainyBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment GrainyBlurFragment
            ENDHLSL
        }
        
        Pass
        {
            name"RadialBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment RaidalBlurFragment
            ENDHLSL
        }

        Pass
        {
            name"DirectionalBlur"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment DirectionalBlurFragment
            ENDHLSL
        }

    }
}