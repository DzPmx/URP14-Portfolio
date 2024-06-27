#ifndef PRE_INTEGRATED_SKIN_LITGHTING_INCLUDED
#define PRE_INTEGRATED_SKIN_LITGHTING_INCLUDED
float4 _GaussianArray[6];
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"

#define CUSTOM_NAMESPACE_START(namespace) struct _##namespace {
#define CUSTOM_NAMESPACE_CLOSE(namespace) }; _##namespace namespace;

CUSTOM_NAMESPACE_START(Common)
    inline half Pow2(half x)
    {
        return x * x;
    }

    inline half Pow4(half x)
    {
        return x * x * x * x;
    }

    inline half Pow5(half x)
    {
        return x * x * x * x * x;
    }

    inline half3 RotateDirection(half3 R, half degrees)
    {
        float3 reflUVW = R;
        half theta = degrees * PI / 180.0f;
        half costha = cos(theta);
        half sintha = sin(theta);
        reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
        return reflUVW;
    }

CUSTOM_NAMESPACE_CLOSE(Common)

CUSTOM_NAMESPACE_START(BxDF)
    ////-----------------------------------------------------------  D  -------------------------------------------------------------------
    // GGX / Trowbridge-Reitz
    // [Walter et al. 2007, "Microfacet models for refraction through rough surfaces"]
    float D_GGX_UE5(float a2, float NoH)
    {
        float d = (NoH * a2 - NoH) * NoH + 1; // 2 mad
        return a2 / (PI * d * d); // 4 mul, 1 rcp
    }

    ////-----------------------------------------------------------  D  -------------------------------------------------------------------

    //----------------------------------------------------------- Vis ----------------------------------------------------------------
    float Vis_Implicit()
    {
        return 0.25;
    }

    // Appoximation of joint Smith term for GGX
    // [Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"]
    float Vis_SmithJointApprox(float a2, float NoV, float NoL)
    {
        float a = sqrt(a2);
        float Vis_SmithV = NoL * (NoV * (1 - a) + a);
        float Vis_SmithL = NoV * (NoL * (1 - a) + a);
        return 0.5 * rcp(Vis_SmithV + Vis_SmithL);
    }

    //----------------------------------------------------------- Vis ----------------------------------------------------------------

    //-----------------------------------------------------------  F -------------------------------------------------------------------
    float3 F_None(float3 SpecularColor)
    {
        return SpecularColor;
    }

    // [Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"]
    float3 F_Schlick_UE5(float3 SpecularColor, float VoH)
    {
        float Fc = Common.Pow5(1 - VoH); // 1 sub, 3 mul
        //return Fc + (1 - Fc) * SpecularColor;		// 1 add, 3 mad

        // Anything less than 2% is physically impossible and is instead considered to be shadowing
        return saturate(50.0 * SpecularColor.g) * Fc + (1 - Fc) * SpecularColor;
    }

    //-----------------------------------------------------------  F -------------------------------------------------------------------

    float3 Diffuse_Lambert(float3 DiffuseColor)
    {
        return DiffuseColor * (1 / PI);
    }

    half3 EnvBRDFApprox(half3 SpecularColor, half Roughness, half NoV)
    {
        // [ Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II" ]
        // Adaptation to fit our G term.
        const half4 c0 = {-1, -0.0275, -0.572, 0.022};
        const half4 c1 = {1, 0.0425, 1.04, -0.04};
        half4 r = Roughness * c0 + c1;
        half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
        half2 AB = half2(-1.04, 1.04) * a004 + r.zw;

        // Anything less than 2% is physically impossible and is instead considered to be shadowing
        // Note: this is needed for the 'specular' show flag to work, since it uses a SpecularColor of 0
        AB.y *= saturate(50.0 * SpecularColor.g);

        return SpecularColor * AB.x + AB.y;
    }

    float3 SpecularGGX(float a2, float3 specular, float NoH, float NoV, float3 NoL, float VoH)
    {
        float D = D_GGX_UE5(a2, NoH);
        float Vis = Vis_SmithJointApprox(a2, NoV, NoL);
        float3 F = F_Schlick_UE5(specular, VoH);

        return (D * Vis) * F;
    }

    float3 SpecularBeckmannLut(float roughness, float VoH, float NoH, float3 H)
    {
        roughness = PerceptualRoughnessToRoughness(roughness);
        float lobe = SAMPLE_TEXTURE2D(_SkinSpecularLut, sampler_SkinSpecularLut, float2(NoH,roughness));
        float PH = pow(2.0 * lobe, 10);
        float3 F = F_Schlick_UE5(0.04, VoH);
        float frSpec = max(PH * F / dot(H, H), 0);
        return frSpec * 20;
    }


    float3 ClearCoatGGX(float roughness, float clearCoat, float NoH, float NoV, float NoL, float VoH, out float3 F)
    {
        float a2 = Common.Pow4(roughness);

        float D = D_GGX_UE5(a2, NoH);
        float Vis = Vis_SmithJointApprox(a2, NoV, NoL);
        F = F_Schlick_UE5(float3(0.04, 0.04, 0.04), VoH) * clearCoat;

        return (D * Vis) * F;
    }

    half3 PreIntegratedSSSBRDF(CustomLitData customLitData, CustomSurfacedata customSurfaceData, half3 L,
                               half3 lightColor,
                               float shadow, float distanceAtten = 1.0)
    {
        half3 H = normalize(customLitData.V + L);
        half NoH = saturate(dot(customLitData.N, H));

        half rNoL = dot(customLitData.NB, L) * 0.5 + 0.5;
        half bNoL = lerp(dot(customLitData.N, L) * 0.5 + 0.5, rNoL, 0.2);
        half gNoL = lerp(rNoL, bNoL, 0.3);

        float2 rUV = float2(rNoL * shadow, customSurfaceData.curvature);
        float2 gUV = float2(gNoL * shadow, customSurfaceData.curvature);
        float2 bUV = float2(bNoL * shadow, customSurfaceData.curvature);

      
        float3 SSS = float3(0.0, 0.0, 0.0);
        SSS.r = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, rUV).r;
        SSS.g = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, gUV).g;
        SSS.b = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, bUV).b;
    
        #if defined(_SSS_OFF)
        SSS=saturate(dot(customLitData.N,L))*shadow;
        #endif

        half VoH = saturate(dot(customLitData.V, H));
        float3 radiance = SSS * lightColor * PI * distanceAtten; //这里给PI是为了和Unity光照系统统一

        float3 diffuseTerm = Diffuse_Lambert(customSurfaceData.albedo);
        #if defined(_DIFFUSE_OFF)
		    diffuseTerm = half3(0,0,0);
        #endif

        //float3 specularTerm = SpecularGGX(a2, customSurfaceData.specular, NoH, NoV, SSS, VoH);
        float3 specularTerm =
            SpecularBeckmannLut(customSurfaceData.roughness, VoH, NoH, H);;
        #if defined(_SPECULAR_OFF)
		    specularTerm = half3(0,0,0);
        #endif
        return (diffuseTerm + specularTerm) * radiance;
    }

    half3 PreIntegratedSSSSpecialBRDF(CustomLitData customLitData, CustomSurfacedata customSurfaceData, half3 L,
                                      half3 lightColor,
                                      float shadow, float distanceAtten = 1.0)
    {
        half3 H = normalize(customLitData.V + L);
        half NoH = saturate(dot(customLitData.N, H));

        half rNoL = dot(customLitData.NB, L) * 0.5 + 0.5;
        half bNoL = lerp(dot(customLitData.N, L) * 0.5 + 0.5, rNoL, 0.2);
        half gNoL = lerp(rNoL, bNoL, 0.3);

        float2 rUV = float2(rNoL * shadow, customSurfaceData.curvature);
        float2 gUV = float2(gNoL * shadow, customSurfaceData.curvature);
        float2 bUV = float2(bNoL * shadow, customSurfaceData.curvature);

        float3 SSS=float3(0.0,0.0,0.0);
        float3 LUT;
        LUT.r = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, rUV).r;
        LUT.g = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, gUV).g;
        LUT.b = SAMPLE_TEXTURE2D(_SkinDiffsueLut, sampler_SkinDiffsueLut, bUV).b;

        for (int i = 0; i < 6; ++i)
        {
         SSS=LUT;   
        }
        #if defined(_SSS_OFF)
    SSS=saturate(dot(customLitData.N,L))*shadow;
        #endif

        half VoH = saturate(dot(customLitData.V, H));
        float3 radiance = SSS * lightColor * PI * distanceAtten; //这里给PI是为了和Unity光照系统统一

        float3 diffuseTerm = Diffuse_Lambert(customSurfaceData.albedo);
        #if defined(_DIFFUSE_OFF)
    diffuseTerm = half3(0,0,0);
        #endif

        //float3 specularTerm = SpecularGGX(a2, customSurfaceData.specular, NoH, NoV, SSS, VoH);
        float3 specularTerm =
            SpecularBeckmannLut(customSurfaceData.roughness, VoH, NoH, H);;
        #if defined(_SPECULAR_OFF)
    specularTerm = half3(0,0,0);
        #endif
        return (diffuseTerm + specularTerm) * radiance;
    }

    half3 EnvBRDF(CustomLitData customLitData, CustomSurfacedata customSurfaceData, float envRotation,
                  float3 positionWS)
    {
        half NoV = saturate(abs(dot(customLitData.N, customLitData.V)) + 1e-5); //区分正反面
        half3 R = reflect(-customLitData.V, customLitData.N);
        R = Common.RotateDirection(R, envRotation);

        //SH
        float3 diffuseAO = GTAOMultiBounce(customSurfaceData.occlusion, customSurfaceData.albedo);
        float3 radianceSH = SampleSH(customLitData.N);
        float3 indirectDiffuseTerm = radianceSH * customSurfaceData.albedo * diffuseAO;
        #if defined(_SH_OFF)
		    indirectDiffuseTerm = half3(0,0,0);
        #endif

        //IBL
        //The Split Sum: 1nd Stage
        half3 specularLD = GlossyEnvironmentReflection(R, positionWS, customSurfaceData.roughness,
                                                       customSurfaceData.occlusion);
        //The Split Sum: 2nd Stage
        half3 specularDFG = EnvBRDFApprox(customSurfaceData.specular, customSurfaceData.roughness, NoV);
        //AO 处理漏光
        float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(
            NoV, customSurfaceData.occlusion, customSurfaceData.roughness);
        float3 specularAO = GTAOMultiBounce(specularOcclusion, customSurfaceData.specular);

        float3 indirectSpecularTerm = specularLD * specularDFG * specularAO;
        #if defined(_IBL_OFF)
		    indirectSpecularTerm = half3(0,0,0);
        #endif
        return indirectDiffuseTerm + indirectSpecularTerm;
    }

CUSTOM_NAMESPACE_CLOSE(BxDF)

CUSTOM_NAMESPACE_START(DirectLighting)
    half3 PreIntegratedSSSShading(CustomLitData customLitData, CustomSurfacedata customSurfaceData, float3 positionWS,
                                  float4 shadowCoord)
    {
        half3 directLighting = (half3)0;
        #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
        	float4 positionCS = TransformWorldToHClip(positionWS);
            shadowCoord = ComputeScreenPos(positionCS);
        #else
        shadowCoord = TransformWorldToShadowCoord(positionWS);
        #endif
        //urp shadowMask是用来考虑烘焙阴影的,因为这里不考虑烘焙阴影所以直接给1
        half4 shadowMask = (half4)1.0;

        //main light
        half3 directLighting_MainLight = (half3)0;
        {
            Light light = GetMainLight(shadowCoord, positionWS, shadowMask);
            half3 L = light.direction;
            half3 lightColor = light.color;
            //SSAO
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(customLitData.ScreenUV);
                lightColor *= aoFactor.directAmbientOcclusion;
            #endif
            half shadow = light.shadowAttenuation;
            directLighting_MainLight = BxDF.PreIntegratedSSSBRDF(customLitData, customSurfaceData, L, lightColor,
                                                                 shadow);
        }

        //add light
        half3 directLighting_AddLight = (half3)0;
        #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();
        for(uint lightIndex = 0; lightIndex < pixelLightCount ; lightIndex++) 
        {
            Light light = GetAdditionalLight(lightIndex,positionWS,shadowMask);
            half3 L = light.direction;
            half3 lightColor = light.color;
            half shadow = light.shadowAttenuation;
            directLighting_AddLight += BxDF.PreIntegratedSSSBRDF(customLitData,customSurfaceData,L,lightColor,shadow,light.distanceAttenuation);                                   
        }
        #endif
        return directLighting_MainLight + directLighting_AddLight;
    }

    half3 PreIntegratedSpecialSSSShading(CustomLitData customLitData, CustomSurfacedata customSurfaceData,
                                         float3 positionWS,
                                         float4 shadowCoord)
    {
        half3 directLighting = (half3)0;
        #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
        	float4 positionCS = TransformWorldToHClip(positionWS);
            shadowCoord = ComputeScreenPos(positionCS);
        #else
        shadowCoord = TransformWorldToShadowCoord(positionWS);
        #endif
        //urp shadowMask是用来考虑烘焙阴影的,因为这里不考虑烘焙阴影所以直接给1
        half4 shadowMask = (half4)1.0;

        //main light
        half3 directLighting_MainLight = (half3)0;
        {
            Light light = GetMainLight(shadowCoord, positionWS, shadowMask);
            half3 L = light.direction;
            half3 lightColor = light.color;
            //SSAO
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(customLitData.ScreenUV);
                lightColor *= aoFactor.directAmbientOcclusion;
            #endif
            half shadow = light.shadowAttenuation;
            directLighting_MainLight = BxDF.PreIntegratedSSSSpecialBRDF(customLitData, customSurfaceData, L, lightColor,
                                                                        shadow);
        }

        //add light
        half3 directLighting_AddLight = (half3)0;
        #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();
        for(uint lightIndex = 0; lightIndex < pixelLightCount ; lightIndex++) 
        {
            Light light = GetAdditionalLight(lightIndex,positionWS,shadowMask);
            half3 L = light.direction;
            half3 lightColor = light.color;
            half shadow = light.shadowAttenuation;
            directLighting_AddLight += BxDF.PreIntegratedSSSSpecialBRDF(customLitData,customSurfaceData,L,lightColor,shadow,light.distanceAttenuation);                                   
        }
        #endif
        return directLighting_MainLight + directLighting_AddLight;
    }

CUSTOM_NAMESPACE_CLOSE(DirectLighting)

CUSTOM_NAMESPACE_START(InDirectLighting)
    half3 EnvShading(CustomLitData customLitData, CustomSurfacedata customSurfaceData, float envRotation,
                     float3 positionWS)
    {
        half3 inDirectLighting = (half3)0;

        inDirectLighting = BxDF.EnvBRDF(customLitData, customSurfaceData, envRotation, positionWS);

        return inDirectLighting;
    }

CUSTOM_NAMESPACE_CLOSE(InDirectLighting)

CUSTOM_NAMESPACE_START(PBR)
    half4 PreIntegratedSSSLit(CustomLitData customLitData, CustomSurfacedata customSurfaceData, float3 positionWS,
                              float4 shadowCoord, float envRotation)
    {
        float3 albedo = customSurfaceData.albedo;
        customSurfaceData.albedo = lerp(customSurfaceData.albedo, float3(0.0, 0.0, 0.0), customSurfaceData.metallic);
        customSurfaceData.specular = lerp(float3(0.04, 0.04, 0.04), albedo, customSurfaceData.metallic);
        half3x3 TBN = half3x3(customLitData.T, customLitData.B, customLitData.N);
        customLitData.N = normalize(mul(customSurfaceData.normalTS, TBN));
        customLitData.NB = normalize(mul(customSurfaceData.normalTSBlur, TBN));

        //SSAO
        #if defined(_SCREEN_SPACE_OCCLUSION)
            AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(customLitData.ScreenUV);
            customSurfaceData.occlusion = min(customSurfaceData.occlusion,aoFactor.indirectAmbientOcclusion);
        #endif

        //DirectLighting
        half3 directLighting = DirectLighting.
            PreIntegratedSSSShading(customLitData, customSurfaceData, positionWS, shadowCoord);

        //IndirectLighting
        half3 inDirectLighting = InDirectLighting.EnvShading(customLitData, customSurfaceData, envRotation, positionWS);
        return half4(directLighting + inDirectLighting, 1);
    }

    half4 PreIntegratedSSSSpecialLit(CustomLitData customLitData, CustomSurfacedata customSurfaceData,
                                     float3 positionWS,
                                     float4 shadowCoord, float envRotation)
    {
        float3 albedo = customSurfaceData.albedo;
        customSurfaceData.albedo = lerp(customSurfaceData.albedo, float3(0.0, 0.0, 0.0), customSurfaceData.metallic);
        customSurfaceData.specular = lerp(float3(0.04, 0.04, 0.04), albedo, customSurfaceData.metallic);
        half3x3 TBN = half3x3(customLitData.T, customLitData.B, customLitData.N);
        customLitData.N = normalize(mul(customSurfaceData.normalTS, TBN));
        customLitData.NB = normalize(mul(customSurfaceData.normalTSBlur, TBN));

        //SSAO
        #if defined(_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(customLitData.ScreenUV);
    customSurfaceData.occlusion = min(customSurfaceData.occlusion,aoFactor.indirectAmbientOcclusion);
        #endif

        //DirectLighting
        half3 directLighting = DirectLighting.
            PreIntegratedSpecialSSSShading(customLitData, customSurfaceData, positionWS, shadowCoord);

        //IndirectLighting
        half3 inDirectLighting = InDirectLighting.EnvShading(customLitData, customSurfaceData, envRotation, positionWS);
        return half4(directLighting + inDirectLighting, 1);
    }

CUSTOM_NAMESPACE_CLOSE(PBR)

#endif
