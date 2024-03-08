Shader "MyURPShader/URP_PostProcessing_Glitch"
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
        #include "ShaderLibrary/MyPostStack.hlsl"
        ENDHLSL

        Pass
        {
            name"RGBSplitHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment RGBSplitHorizontalFragment
            ENDHLSL
        }

        Pass
        {
            name"RGBSplitVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment RGBSplitVerticalFragment
            ENDHLSL
        }
        Pass
        {
            name"RGBSplitCombine"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment RGBSplitCombineFragment
            ENDHLSL
        }
        Pass
        {
            name"ImageBlock"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment ImageBlockFragment
            ENDHLSL
        }

        Pass
        {
            name"LineBlockHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment LineBlockHorizontalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }

        Pass
        {
            name"LineBlockVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment LineBlockVerticalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }

        Pass
        {
            name"TileJitterHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment TileJitterHorizontalFragment
            #pragma shader_feature JITTER_DIRECTION_HORIZONTAL
	        #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }

        Pass
        {
            name"TileJitterVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment TileJitterVerticalFragment
            #pragma shader_feature JITTER_DIRECTION_HORIZONTAL
	        #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }
        
        Pass
        {
            name"ScanLineJitterHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment ScanLineJitterHorizontalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }
        
        Pass
        {
            name"ScanLineJitterVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment ScanLineJitterVerticalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }
        
        Pass
        {
            name"DigitalStripe"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment DigitalStripeFragment
            #pragma shader_feature NEED_TRASH_FRAME
            ENDHLSL
        }
        
        Pass
        {
            name"AnalogNoise"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment AnalogNoiseFragment
            ENDHLSL
        }
        
        Pass
        {
            name"WaveJitterHorizontal"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment WaveJitterHorizontalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }
        Pass
        {
            name"WaveJitterVertical"
            Cull Off
            Zwrite Off

            HLSLPROGRAM
            #pragma vertex triangleDrawVert
            #pragma fragment WaveJitterVerticalFragment
            #pragma shader_feature USING_FREQUENCY_INFINITE
            ENDHLSL
        }
    }
}