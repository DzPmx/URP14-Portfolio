Shader"MyURPShader/Environment Rendering /CloudSea"
{
    Properties
    {
        _NoiseMap ("NoiseMap", 3D) = "white" {}
        _DensityMap ("DensityMap", 3D) = "white" {}
        _FogSkyColorSampler ("FogSkyColorSampler", 2D) = "white" {}
        _BetaRay ("BetaRay", Vector) = (0.00005,0.00011,0.00027,0.0)
        _BetaMie ("BetaMie", Vector) = (0.00000351482,0.00000532746,0.00000820421,120)
        _HeightFogData("HeightFogData", Vector) = (0.002,0.004,0.0,0.0)
        _ZFadeParam("ZFadeParam", Vector) = (0.33333,0.00033,3.0,0.01)
        [Toggle]_UseSegFogAdjust("UseSegFogAdjust",int)=0
        _FogIntensityScale("FogIntensityScale", Vector) = (1.0,1.0,1.0,1.0)
        _QSMainLightColor("QSMainLightColor", Color) = (1.61901,1.16672,0.86062,0.5)
        _Slices("Slices", Float) = 40.0
        _Tilling("Tilling", Vector) = (0.0015,0.02,0.0015,0.0)
        _Bound("Bound", Vector) = (-54.51778,-2.26779,0.0,-40.66007)
        _Radius("Radius", Vector) = (1300,1300,0.0,0.0)
        _Density("Density", Vector) = (0.003,1.0,0.0,0.0)
        _SoftEdge("SoftEdge", Float) =0.01
        _LightIntensity("LightIntensity", Vector) = (1.0,10.00,0.0,0.0)
        _CloudSeaGreenstein("CloudSeaGreenstein", Vector) = (0.01512,1.81,1.80,0.0)
        _DiffuseColor("DiffuseColor", Color) = (0.83088,0.88803,1.0,0.516)
        _NoiseTilling("NoiseTilling", Float) = 0.01
        _NoiseIntensity("NoiseIntensity", Float) = 15.00
        _VolumeTransform("VolumeTransform", Vector) = (-10542.46582,-10526.55566,-5271.23291,-5263.27783)

    }
    SubShader
    {
        Tags
        {
            "Queue" = "transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_FogSkyColorSampler);
            SAMPLER(sampler_FogSkyColorSampler);
            TEXTURE3D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            TEXTURE3D(_DensityMap);
            SAMPLER(sampler_DensityMap);
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BetaRay;
                float4 _BetaMie;
                float4 _HeightFogData;
                float4 _ZFadeParam;
                int _UseSegFogAdjust;
                float4 _FogIntensityScale;
                float4 _QSMainLightColor;
                float _Slices;
                float3 _Tilling;
                float4 _Bound;
                float2 _Radius;
                float2 _Density;
                float _SoftEdge;
                float2 _LightIntensity;
                float3 _CloudSeaGreenstein;
                float4 _DiffuseColor;
                float _NoiseIntensity;
                float _NoiseTilling;
                float4 _VolumeTransform;
            CBUFFER_END

            #define INV_LN2 1.442695
            float4 ImmCB_0_0_0[4];
            float2 u_xlat0;
            //float3 u_xlat1;
            //float3 u_xlat16_2;
            float3 u_xlat16_3;
            float2 u_xlati4;
            float3 u_xlat16_5;
            float4 u_xlat16_6;
            float4 u_xlat16_7;
            float4 u_xlat8;
            float4 u_xlat16_8;

            float4 u_xlat9;
            float4 u_xlat16_9;
            float4 u_xlat10_9;
            float4 u_xlat16_10;
            float3 u_xlat10_10;

            float u_xlat16_18;
            float u_xlat22;
            float2 u_xlat26;
            float u_xlat16_26;
            int u_xlati26;

            float u_xlat16_29;
            float u_xlat33;
            float u_xlat16_33;
            int u_xlati33;

            int u_xlati34;

            float u_xlat35;

            float u_xlat16_36;
            float u_xlat37;

            float u_xlat16_38;

          

            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 texcoord0 : TEXCOORD0;
                float4 posCS : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.vertex);
                float3 posWS = TransformObjectToWorld(v.vertex);
                float3 posVS = TransformWorldToView(posWS);
                o.texcoord0.xyz = posWS.xyz;
                o.texcoord0.w = -posVS.z;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                _VolumeTransform-=_Time.z;
                bool u_xlatb8;
                bool u_xlatb11;
                bool u_xlatb26;
                bool u_xlatb33;
                bool u_xlatb35;
                bool u_xlatb37;
                (ImmCB_0_0_0[0] = float4(1.0, 0.0, 0.0, 0.0));
                (ImmCB_0_0_0[1] = float4(0.0, 1.0, 0.0, 0.0));
                (ImmCB_0_0_0[2] = float4(0.0, 0.0, 1.0, 0.0));
                (ImmCB_0_0_0[3] = float4(0.0, 0.0, 0.0, 1.0));
                float2 screenUV=i.posCS / _ScreenParams.xy;

                float3 posWS=normalize(i.texcoord0.xyz);
                float3 viewDirection=normalize(i.texcoord0.xyz-_WorldSpaceCameraPos);
  
                
                float3 fogskyColor= SAMPLE_TEXTURE2D_LOD(_FogSkyColorSampler, sampler_FogSkyColorSampler, screenUV.xy, 0.0);
                //maxviewZ
                (u_xlat22 = min(_ProjectionParams.z -_Radius.y, _Bound.w / viewDirection.y));
                //slicePosition  雾效切片的位置
                (u_xlat16_3.xy = (_Bound.yx / (viewDirection.yy)));

               
                //是否在Bound范围中
                (u_xlatb33 = (u_xlat16_3.x >= 0.0)&&(u_xlat16_3.x < _Radius.y));
                //起始切片计算
                u_xlat16_3.x = sqrt(u_xlat16_3.x / _Radius.x)*_Slices;
                u_xlati4.x = u_xlat16_3.x;
                //终止切片计算
                u_xlat16_3.x = ceil(sqrt(min(u_xlat16_3.y, _Radius.y)/_Radius.y)*_Slices);
                u_xlati4.y = u_xlat16_3.x;
                //有效性
                (u_xlati4.xy = ((bool(u_xlatb33)) ? (u_xlati4.xy) : (float2(-1, -1))));
                (u_xlat33 = float(u_xlati4.x));
                
                (u_xlatb33 = (u_xlat33 < 0.0));
                if (u_xlatb33 * -1 != 0)
                {
                    discard; 
                }
                Light light = GetMainLight();
                float3 fogColor = _QSMainLightColor.xyz * _DiffuseColor.xyz;
                (u_xlat33 = dot(posWS.xyz, light.direction.xyz));
              
                //phase
                u_xlat16_36 = -_CloudSeaGreenstein.z * -u_xlat33+ _CloudSeaGreenstein.y;
                u_xlat16_33 = _CloudSeaGreenstein.x/exp2(log2(u_xlat16_36)*1.5);
                
                (u_xlat16_5.xyz = (fogColor * u_xlat16_33));
                (u_xlat16_6.xyz = (fogColor* _LightIntensity.xxx));
                (u_xlat16_5.xyz = ((u_xlat16_5.xyz * _LightIntensity.y) + u_xlat16_6.xyz));
                (u_xlat16_3.xyz = (fogColor.xyz * _DiffuseColor.www));
                
                float cameraDepth= LinearEyeDepth(SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV, 0.0).x,_ZBufferParams);

                //视线Z值小于cameradepth
                (u_xlatb11 = (i.texcoord0.w < cameraDepth));
                (u_xlat16_36 = (_Density.x * 10.0));
                (u_xlati33 = _UseSegFogAdjust);
                (u_xlat16_6= 0.0);
                (u_xlati34 = u_xlati4.x);
               
                while (true)
                {
                    (u_xlatb35 = (u_xlati34 >= u_xlati4.y));
                    if (u_xlatb35)
                    {
                        break;
                    }
                    u_xlat16_7.x = Sq(float(u_xlati34)/_Slices);
                    
                    (u_xlat16_18 = (u_xlat16_7.x * _Radius.x));
                    (u_xlat35 = ((_Radius.x * u_xlat16_7.x) + u_xlat22));
                    u_xlatb26 = u_xlatb11 && (cameraDepth < u_xlat35);
                    if (u_xlatb26)
                    {
                        break;
                    }
                    u_xlat8.xz = viewDirection.xz * u_xlat16_18;
                 
                    (u_xlat9.xy = ((viewDirection.xz * u_xlat16_18) + _VolumeTransform.zw));
                     u_xlat9.z = viewDirection.y * u_xlat16_18-_Bound.x;
                    (u_xlat9.xyw = (u_xlat9.xyz * _NoiseTilling));
                    (u_xlat10_10.xyz = SAMPLE_TEXTURE3D_LOD(_NoiseMap, sampler_NoiseMap, u_xlat9.xyw, 0.0).xyz);
                    u_xlat16_10.xyz = u_xlat10_10.xyz -0.5;
                    (u_xlat9.xyw = (u_xlat9.xyw * 4));
                    float3 noise = ((SAMPLE_TEXTURE3D_LOD(_NoiseMap, sampler_NoiseMap, u_xlat9.xyw, 0.0).xyz -0.5)*0.5+u_xlat16_10.xyz)* _NoiseIntensity;
                    u_xlat26.x = u_xlat9.z * _Tilling.y;
                    u_xlat26.x = u_xlat26.x * 0.25 + 0.75;
                    (u_xlat8.w = u_xlat9.z);
                    (u_xlat8.xyz = ((u_xlat26.xxx * noise.xyz) + u_xlat8.xzw));
                    (u_xlat8.xy = (u_xlat8.xy + _VolumeTransform.xy));
                    (u_xlat8.xyw = float3((u_xlat8.x * _Tilling.x), (u_xlat8.y * float(_Tilling.z)),(u_xlat8.z * _Tilling.y)));
                    (u_xlatb26 = (0.0 < u_xlat8.w));
                    (u_xlatb37 = (u_xlat8.w < 1.0));
                    (u_xlatb26 = (u_xlatb37 && u_xlatb26));
                    (u_xlat8.xyw = SAMPLE_TEXTURE3D_LOD(_DensityMap, sampler_DensityMap, u_xlat8.xyw, 0.0).xyz);
                    (u_xlat8.xyw = lerp(float3(0.0, 0.0, 0.0), u_xlat8.xyw, (u_xlatb26)));
                    (u_xlatb26 = (0.0 >= u_xlat8.x));
                    if (u_xlatb26)
                    {

                        (u_xlati26 = (u_xlati34 + 1));
                        (u_xlat16_38 = u_xlat16_18);
                        (u_xlati34 = u_xlati26);
                        continue;
                    }
                    (u_xlat16_26 = ((_Radius.x * u_xlat16_7.x) + (-u_xlat16_38)));
                    u_xlat16_7.x = clamp(abs(cameraDepth-u_xlat35)*_SoftEdge,0.0,1.0)*u_xlat8.x;
                    (u_xlat16_29 = (u_xlat16_36 * u_xlat16_26));
                    (u_xlat16_7.x = (u_xlat16_29 * u_xlat16_7.x));
                    u_xlat26.x = max(u_xlat35 -_BetaMie.w,0.0);
                    u_xlat37 = u_xlat35 * u_xlat8.z* _HeightFogData.z;
                    (u_xlatb8 = (6.1999999e-05 < abs(u_xlat37)));
                    u_xlat9.x = exp2(u_xlat37 * -1.442695)*(-u_xlat26.x)+ u_xlat26.x;
                    (u_xlat37 = (u_xlat9.x / u_xlat37));
                    (u_xlat37 = ((u_xlatb8) ? (u_xlat37) : (u_xlat26.x)));
                    (u_xlat37 = (u_xlat37 * (-_HeightFogData.w)));
                    (u_xlat37 = (u_xlat37 * 1.442695));
                    (u_xlat37 = exp2(u_xlat37));
                    (u_xlat8.x = ((u_xlat35 * _ZFadeParam.y) + (-_ZFadeParam.x)));
                    (u_xlat35 = ((u_xlat35 * u_xlat8.z) +_WorldSpaceCameraPos.y));
                    (u_xlat35 = (((-u_xlat35) * _ZFadeParam.w) + _ZFadeParam.z));
                    (u_xlat35 = max(u_xlat35, 0.0));
                    (u_xlat35 = ((u_xlat8.x * u_xlat35) + u_xlat8.x));
                    (u_xlat35 = clamp(u_xlat35, 0.0, 1.0));
                    (u_xlat8.x = (u_xlat35 * u_xlat35));
                    (u_xlat9.x = ((-u_xlat26.x) + 10000.0));
                    (u_xlat26.x = ((u_xlat8.x * u_xlat9.x) + u_xlat26.x));
                    u_xlat35 = 1.0-u_xlat35;
                    u_xlat35*=u_xlat35*u_xlat37;
                    (u_xlat37 = ((u_xlat26.x * u_xlat8.z) + (-_BetaRay.w)));
                    (u_xlat8.xz = (u_xlat37 * _HeightFogData.xy));
                    (u_xlatb37 = (6.1999999e-05 < abs(u_xlat8.z)));
                    (u_xlat9.xy = (u_xlat8.xz * -INV_LN2));
                    (u_xlat9.xy = exp2(u_xlat9.xy));
                    (u_xlat9.xy = (((-u_xlat26.xx) * u_xlat9.xy) + u_xlat26.xx));
                    (u_xlat8.xz = (u_xlat9.xy / u_xlat8.xz));
                    (u_xlat26.xy = ((bool(u_xlatb37)) ? (u_xlat8.xz) : (u_xlat26.xx)));
                    (u_xlat9.xyz = (u_xlat26.yyy * _BetaMie.xyz));
                    (u_xlat9.xyz = ((_BetaRay.xyz * u_xlat26.xxx) + u_xlat9.xyz));
                    (u_xlatb26 = (0 < u_xlati33));
                    if (u_xlatb26)
                    {
                        (u_xlati26 = (u_xlati33 + -1));
                        (u_xlat26.x = dot(_FogIntensityScale, ImmCB_0_0_0[u_xlati26]));
                        (u_xlat9.xyz = (u_xlat26.xxx * (-u_xlat9.xyz)));
                    }
                    else
                    {
                        u_xlat9.xyz = -u_xlat9.xyz;
                    }
                    u_xlat9.xyz = u_xlat9.xyz * INV_LN2;
                    u_xlat9.xyz = exp2(u_xlat9.xyz);
                    u_xlat16_7.x = u_xlat16_7.x * -INV_LN2;
                    u_xlat16_7.x = exp2(u_xlat16_7.x);
                    u_xlat16_10.w = -u_xlat16_7.x* u_xlat35 + u_xlat35;
                    u_xlat16_7.xzw = u_xlat16_3.xyz * u_xlat8.yyy;
                    u_xlat16_7.xzw = u_xlat8.www * u_xlat16_5.xyz + u_xlat16_7.xzw;
                    u_xlat16_7.xzw = -fogskyColor.xyz + u_xlat16_7.xzw;
                    u_xlat16_7.xzw = u_xlat9.xyz * u_xlat16_7.xzw+ fogskyColor.xyz;
                    u_xlat16_10.xyz = u_xlat16_10.www * u_xlat16_7.xzw;
                    u_xlat16_7.x = (-u_xlat16_6.w) + 1.0;
                    u_xlat16_8 = u_xlat16_7.xxxx * u_xlat16_10 + u_xlat16_6;
                    u_xlatb35 = u_xlat16_8.w >= 1.0;
                    if (u_xlatb35)
                    {
                        u_xlat16_6 = u_xlat16_8;
                        break;
                    }
                    u_xlat16_6 = u_xlat16_8;
                    u_xlat16_38 = u_xlat16_18;
                    u_xlati34 = u_xlati34 + 1;
                   
                }
                return u_xlat16_6;
            }
            ENDHLSL
        }
    }
}