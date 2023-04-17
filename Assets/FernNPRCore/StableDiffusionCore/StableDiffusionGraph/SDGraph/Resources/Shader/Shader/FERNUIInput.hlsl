#ifndef UNIVERSAL_NPRSTANDARD_INPUT_INCLUDED
#define UNIVERSAL_NPRSTANDARD_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../ShaderLibrary/NPRSurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#include "../ShaderLibrary/NPRInput.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

TEXTURE2D(_DiffuseRampMap);				SAMPLER(sampler_DiffuseRampMap);

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half4 _AnisoDetailMap_ST;
half4 _MatCapTex_ST;
half4 _BaseColor;
half4 _HighColor;
half4 _DarkColor;
half4 _SpecularColor;
half4 _RimColor;
half4 _AnisoSpecularColor;
half4 _AnisoSecondarySpecularColor;
half4 _AngleRingBrightColor;
half4 _AngleRingShadowColor;
half4 _MatCapColor;
half4 _EmissionColor;

// Channel
half4 _PBRMetallicChannel;
half4 _PBRSmothnessChannel;
half4 _PBROcclusionChannel;
half4 _SpecularIntensityChannel;
half4 _EmissionChannel;

#if EYE
    half4 _EyeParallaxChannel;
#endif

half4 _OutlineColor;

half _LightDirectionObliqueWeight;
half _BumpScale;
half _Metallic;
half _Smoothness;
half _OcclusionStrength;
half _ClearCoatMask;
half _ClearCoatSmoothness;
half _UseHalfLambert;
half _UseRadianceOcclusion;
half _CELLThreshold;
half _CELLSmoothing;
half _CellBandSoftness;
half _CellBands;
half _RampMapUOffset;
half _RampMapVOffset;
half _StylizedSpecularSize;
half _StylizedSpecularSoftness;
half _StylizedSpecularAlbedoWeight;

// specular
half _Shininess;
half _SpecularAAThreshold;
half _SpaceScreenVariant;

half _AnisoShiftScale;
half _AnsioSpeularShift;
half _AnsioSecondarySpeularShift;
half _AnsioSpeularStrength;
half _AnsioSecondarySpeularStrength;
half _AnsioSpeularExponent;
half _AnsioSecondarySpeularExponent;
half _AnisoSpread1;
half _AnisoSpread2;
half _AngleRingWidth;
half _AngleRingSoftness;
half _AngleRingThreshold;
half _AngleRingIntensity;
half _EmissionColorAlbedoWeight;

// depth shadow
half _DepthOffsetShadowReverseX;
half _DepthOffsetRimReverseX;
half _DepthShadowOffset;
half _DepthShadowThresoldOffset;
half _DepthShadowSoftness;

// rim
half _RimDirectionLightContribution;
half _RimThreshold;
half _RimSoftness;
half _DepthRimOffset;
half _DepthRimThresoldOffset;

half _ZOffset;

#if EYE
    half _Parallax;
    half _BumpIrisInvert;
    half _MatCapAlbedoWeight;
#endif

#if FACE
half _SDFFaceArea;
half _SDFDirectionReversal;
half _SDFShadingSoftness;
#endif

half _LightIntensityClamp;
half _Is_Filter_LightColor;

// Surface
half _Cutoff;
half _Surface;
half _ClipThresold;

// AI
half _Is_SDInPaint;

half _OutlineWidth;
CBUFFER_END



// NOTE: Do not ifdef the properties for dots instancing, but ifdef the actual usage.
// Otherwise you might break CPU-side as property constant-buffer offsets change per variant.
// NOTE: Dots instancing is orthogonal to the constant buffer above.
// #ifdef UNITY_DOTS_INSTANCING_ENABLED
// UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
//     UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
//     UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
//     UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
//     UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
//     UNITY_DOTS_INSTANCED_PROP(float4, _AnisoSpecularColor)
//     UNITY_DOTS_INSTANCED_PROP(float4, _AnisoSecondarySpecularColor)
//     UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
//     UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
//     UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
//     UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
//     UNITY_DOTS_INSTANCED_PROP(float , _UseHalfLambert)
//     UNITY_DOTS_INSTANCED_PROP(float , _UseRadianceOcclusion)
//     UNITY_DOTS_INSTANCED_PROP(float , _CELLThreshold)
//     UNITY_DOTS_INSTANCED_PROP(float , _CELLSmoothing)
//     UNITY_DOTS_INSTANCED_PROP(float , _RampMapUOffset)
//     UNITY_DOTS_INSTANCED_PROP(float , _RampMapVOffset)
//     UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularSize)
//     UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularSoftness)
//     UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularAlbedoWeight)
//     UNITY_DOTS_INSTANCED_PROP(float , _Shininess)
//     UNITY_DOTS_INSTANCED_PROP(float , _RimDirectionLightContribution)
//     UNITY_DOTS_INSTANCED_PROP(float , _RimThreshold)
//     UNITY_DOTS_INSTANCED_PROP(float , _RimSoftness)
//     UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimWidth)
//     UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimThreshold)
//     UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimSoftness)
//     UNITY_DOTS_INSTANCED_PROP(float , _AnisoShiftScale)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularShift)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularShift)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularStrength)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularStrength)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularExponent)
//     UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularExponent)
//     UNITY_DOTS_INSTANCED_PROP(float , _anisoSpread1)
//     UNITY_DOTS_INSTANCED_PROP(float , _anisoSpread2)
//     UNITY_DOTS_INSTANCED_PROP(float , _Parallax)
//     UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
//     UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatMask)
//     UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatSmoothness)
//     UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoMapScale)
//     UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalMapScale)
//     UNITY_DOTS_INSTANCED_PROP(float , _PBRMetallicChannel)
//     UNITY_DOTS_INSTANCED_PROP(float , _PBRSmothnessChannel)
//     UNITY_DOTS_INSTANCED_PROP(float , _PBROcclusionChannel)
//     UNITY_DOTS_INSTANCED_PROP(float , _Surface)
// UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
//
// #define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BaseColor)
// #define _SpecColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_SpecColor)
// #define _EmissionColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_EmissionColor)
// #define _RimColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimColor)
// #define _Cutoff                 UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Cutoff)
// #define _BumpScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , _BumpScale)
// #define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Smoothness)
// #define _Metallic               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Metallic)
// #define _UseHalfLambert         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_UseHalfLambert)
// #define _UseRadianceOcclusion         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_UseRadianceOcclusion
// #define _CELLThreshold          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_CELLThreshold)
// #define _RampMapUOffset         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RampMapUOffset)
// #define _RampMapVOffset         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RampMapVOffset)
// #define _StylizedSpecularSize       UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_UseHalfLambert)
// #define _StylizedSpecularSoftness   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_StylizedSpecularSoftness)
// #define _StylizedSpecularAlbedoWeight   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_StylizedSpecularAlbedoWeight)
// #define _Shininess   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Shininess)
// #define _RimDirectionLightContribution              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimDirectionLightContribution)
// #define _RimThreshold              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimThreshold)
// #define _RimSoftness              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimSoftness)
// #define _ScreenSpaceRimWidth              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimWidth)
// #define _ScreenSpaceRimThreshold              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimThreshold)
// #define _ScreenSpaceRimSoftness              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimSoftness)
// #define _AnisoShiftScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_AnisoShiftScale)
// #define _ansioSpeularShift              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularShift)
// #define _ansioSecondarySpeularShift              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularShift)
// #define _ansioSpeularStrength              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularStrength)
// #define _ansioSecondarySpeularStrength              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularStrength)
// #define _ansioSpeularExponent              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularExponent)
// #define _ansioSecondarySpeularExponent              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularExponent)
// #define _anisoSpread1              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_anisoSpread1)
// #define _anisoSpread2              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_anisoSpread2)
// #define _Parallax               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Parallax)
// #define _OcclusionStrength      UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_OcclusionStrength)
// #define _ClearCoatMask          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ClearCoatMask)
// #define _ClearCoatSmoothness    UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ClearCoatSmoothness)
// #define _DetailAlbedoMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_DetailAlbedoMapScale)
// #define _DetailNormalMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_DetailNormalMapScale)
// #define _PBRSmothnessChannel   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_PBRSmothnessChannel)
// #define _OcclusionStrength   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_OcclusionStrength)
// #define _PBROcclusionChannel   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_PBROcclusionChannel)
// #define _Surface                UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Surface)
// #endif

TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_MatCapTex);       SAMPLER(sampler_MatCapTex);
TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);
TEXTURE2D(_LightMap);           SAMPLER(sampler_LightMap);
TEXTURE2D(_AnisoShiftMap);       SAMPLER(sampler_AnisoShiftMap);
TEXTURE2D(_ShadingMap01);       SAMPLER(sampler_ShadingMap01);
TEXTURE2D(_EmissionTex);       SAMPLER(sampler_EmissionTex);

#if FACE
    TEXTURE2D(_SDFFaceTex);      SAMPLER(sampler_SDFFaceTex);
#endif


#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

/**
 * \brief This Function only for Uber version
 * \param channel 
 * \return 
 */
half GetVauleFromChannel(half4 pbrLightMap, half4 shadingMap01, half4 channel)
{
    int index = length(channel);
    half4 channelMap = pbrLightMap;
    if(index == 2)
    {
        channelMap = shadingMap01;
    }
    channel /= index;
    return dot(channelMap, channel);
}

/**
 * \brief This Function only for Uber version
 * \param uv 
 * \param lightMap 
 * \param sampler_lightMap 
 * \return 
 */
half4 SamplePBRChannel(half4 pbrLightMap, half4 shadingMap01)
{
    half4 pbrChannel = 1;
    pbrChannel.r = GetVauleFromChannel(pbrLightMap, shadingMap01, _PBRMetallicChannel);
    pbrChannel.g = GetVauleFromChannel(pbrLightMap, shadingMap01, _PBROcclusionChannel);
    pbrChannel.a = GetVauleFromChannel(pbrLightMap, shadingMap01, _PBRSmothnessChannel);
    return pbrChannel;
}

half3 EmissionColor(half4 pbrLightMap, half4 shadingMap01, half3 albedo, half2 uv)
{
    half3 emissionColor = 0;
    #if _USEEMISSIONTEX
        emissionColor = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, uv).rgb;
    #else
        half emissionChannel = GetVauleFromChannel(pbrLightMap, shadingMap01, _EmissionChannel);
        emissionColor = emissionChannel;
    #endif
    emissionColor *= lerp(_EmissionColor.rgb, _EmissionColor.rgb * albedo, _EmissionColorAlbedoWeight);
    return emissionColor;
}

#if EYE
    half2 EasyParallaxOffset(half parallax, half3x3 TBN, half3 viewDirWS)
    {
        half3 tangentViewDir = SafeNormalize(mul(TBN, viewDirWS));
        half paraMask = parallax * 0.15f;
        half2 parallxOffset = clamp(tangentViewDir.xy / (tangentViewDir.z + 0.42f) * _Parallax * paraMask, -paraMask, paraMask);
        return parallxOffset;
    }
#endif

inline half SpecularAA(half3 normalWS, half smoothness)
{
    half dx = dot(ddx(normalWS),ddx(normalWS));
    half dy = dot(ddy(normalWS),ddy(normalWS));
    half roughness = 1 - smoothness;
    half roughnessAA = roughness * roughness + min(_SpaceScreenVariant * (dx + dy) * 2, _SpecularAAThreshold * _SpecularAAThreshold);
    roughnessAA = saturate(roughnessAA);
    roughnessAA = sqrt(roughnessAA);
    roughnessAA = sqrt(roughnessAA);
    half smoothnessAA = 1 - roughnessAA;
    return smoothnessAA;
}

inline void InitializeNPRStandardSurfaceData(float2 uv, InputData inputData, out NPRSurfaceData outSurfaceData)
{
    outSurfaceData = (NPRSurfaceData)0;
    half4 shadingMap01 = SAMPLE_TEXTURE2D(_ShadingMap01, sampler_ShadingMap01, uv);
    half2 uvOffset = 0;
    #if EYE
        outSurfaceData.parallax = dot(shadingMap01, _EyeParallaxChannel);
        uvOffset = EasyParallaxOffset( outSurfaceData.parallax, inputData.tangentToWorld, inputData.viewDirectionWS);
    #endif
    uv += uvOffset;
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half4 pbrLightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, uv);
    half4 pbrChannel = SamplePBRChannel(pbrLightMap, shadingMap01);
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.smoothness = _Smoothness * pbrChannel.a;
    outSurfaceData.metallic = _Metallic * pbrChannel.r;
    outSurfaceData.occlusion = LerpWhiteTo(pbrChannel.g, _OcclusionStrength);
    outSurfaceData.clearCoatMask = _ClearCoatMask;
    outSurfaceData.clearCoatSmoothness = _ClearCoatSmoothness;
    #if _SPECULARMASK
    outSurfaceData.specularIntensity = GetVauleFromChannel(pbrLightMap, shadingMap01, _SpecularIntensityChannel);
    #else
    outSurfaceData.specularIntensity = 1;
    #endif
    outSurfaceData.emission = EmissionColor(pbrLightMap, shadingMap01, outSurfaceData.albedo, uv);

   
}


inline void InitializeNPRStandardSurfaceData(float2 uv, out NPRSurfaceData outSurfaceData)
{
    outSurfaceData = (NPRSurfaceData)0;
    half4 shadingMap01 = SAMPLE_TEXTURE2D(_ShadingMap01, sampler_ShadingMap01, uv);
    half2 uvOffset = 0;
    uv += uvOffset;
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half4 pbrLightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, uv);
    half4 pbrChannel = SamplePBRChannel(pbrLightMap, shadingMap01);
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.smoothness = _Smoothness * pbrChannel.a;
    outSurfaceData.metallic = _Metallic * pbrChannel.r;
    outSurfaceData.occlusion = LerpWhiteTo(pbrChannel.g, _OcclusionStrength);
    outSurfaceData.clearCoatMask = _ClearCoatMask;
    outSurfaceData.clearCoatSmoothness = _ClearCoatSmoothness;
    outSurfaceData.specularIntensity = GetVauleFromChannel(pbrLightMap, shadingMap01, _SpecularIntensityChannel);
    outSurfaceData.emission = EmissionColor(pbrLightMap, shadingMap01, outSurfaceData.albedo, uv);
}

inline void InitAnisoSpecularData(out AnisoSpecularData anisoSpecularData)
{
    anisoSpecularData.specularColor = _AnisoSpecularColor.rgb;
    anisoSpecularData.specularSecondaryColor = _AnisoSecondarySpecularColor.rgb;
    anisoSpecularData.specularShift = _AnsioSpeularShift;
    anisoSpecularData.specularSecondaryShift  = _AnsioSecondarySpeularShift;
    anisoSpecularData.specularStrength = _AnsioSpeularStrength;
    anisoSpecularData.specularSecondaryStrength = _AnsioSecondarySpeularStrength;
    anisoSpecularData.specularExponent = _AnsioSpeularExponent;
    anisoSpecularData.specularSecondaryExponent = _AnsioSecondarySpeularExponent;
    anisoSpecularData.spread1 = _AnisoSpread1;
    anisoSpecularData.spread2 = _AnisoSpread2;
}

inline void InitAngleRingSpecularData(half mask, out AngleRingSpecularData angleRingSpecularData)
{
    angleRingSpecularData.shadowColor = _AngleRingShadowColor.rgb;
    angleRingSpecularData.brightColor = _AngleRingBrightColor.rgb;
    angleRingSpecularData.mask = mask;
    angleRingSpecularData.width = _AngleRingWidth;
    angleRingSpecularData.softness = _AngleRingSoftness;
    angleRingSpecularData.threshold = _AngleRingThreshold;
    angleRingSpecularData.intensity = _AngleRingIntensity;
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
