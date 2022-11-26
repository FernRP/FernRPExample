#ifndef UNIVERSAL_NPRSTANDARD_INPUT_INCLUDED
#define UNIVERSAL_NPRSTANDARD_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../ShaderLibrary/NPRSurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "../ShaderLibrary/NPRLighting.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

TEXTURE2D(_DiffuseRampMap);				SAMPLER(sampler_DiffuseRampMap);

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half4 _AnisoDetailMap_ST;
half4 _BaseColor;
half4 _HighColor;
half4 _DarkColor;
half4 _SpecularColor;
half4 _RimColor;
half4 _AnisoSpecularColor;
half4 _AnisoSecondarySpecularColor;
half _BumpScale;
half _Smoothness;
half _OcclusionStrength;
half _Metallic;
half _UseHalfLambert;
half _CELLThreshold;
half _CELLSmoothing;
half _CellBandSoftness;
half _CellBands;
half _RampMapUOffset;
half _RampMapVOffset;
half _StylizedSpecularSize;
half _StylizedSpecularSoftness;
half _StylizedSpecularAlbedoWeight;
half _Shininess;
half _RimDirectionLightContribution;
half _RimThreshold;
half _RimSoftness;
half _ScreenSpaceRimWidth;
half _ScreenSpaceRimThreshold;
half _ScreenSpaceRimSoftness;
half _AnisoShiftScale;
half _AnsioSpeularShift;
half _AnsioSecondarySpeularShift;
half _AnsioSpeularStrength;
half _AnsioSecondarySpeularStrength;
half _AnsioSpeularExponent;
half _AnsioSecondarySpeularExponent;
half _AnisoSpread1;
half _AnisoSpread2;

// Surface
half _Cutoff;
half _Surface;
half _ClipThresold;
CBUFFER_END

// NOTE: Do not ifdef the properties for dots instancing, but ifdef the actual usage.
// Otherwise you might break CPU-side as property constant-buffer offsets change per variant.
// NOTE: Dots instancing is orthogonal to the constant buffer above.
#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _AnisoSpecularColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _AnisoSecondarySpecularColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _UseHalfLambert)
    UNITY_DOTS_INSTANCED_PROP(float , _CELLThreshold)
    UNITY_DOTS_INSTANCED_PROP(float , _CELLSmoothing)
    UNITY_DOTS_INSTANCED_PROP(float , _RampMapUOffset)
    UNITY_DOTS_INSTANCED_PROP(float , _RampMapVOffset)
    UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularSize)
    UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _StylizedSpecularAlbedoWeight)
    UNITY_DOTS_INSTANCED_PROP(float , _Shininess)
    UNITY_DOTS_INSTANCED_PROP(float , _RimDirectionLightContribution)
    UNITY_DOTS_INSTANCED_PROP(float , _RimThreshold)
    UNITY_DOTS_INSTANCED_PROP(float , _RimSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimWidth)
    UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimThreshold)
    UNITY_DOTS_INSTANCED_PROP(float , _ScreenSpaceRimSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _AnisoShiftScale)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularShift)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularShift)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSpeularExponent)
    UNITY_DOTS_INSTANCED_PROP(float , _ansioSecondarySpeularExponent)
    UNITY_DOTS_INSTANCED_PROP(float , _anisoSpread1)
    UNITY_DOTS_INSTANCED_PROP(float , _anisoSpread2)
    UNITY_DOTS_INSTANCED_PROP(float , _Parallax)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatMask)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatSmoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BaseColor)
#define _SpecColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_SpecColor)
#define _EmissionColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_EmissionColor)
#define _RimColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimColor)
#define _Cutoff                 UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Cutoff)
#define _BumpScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , _BumpScale)
#define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Smoothness)
#define _Metallic               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Metallic)
#define _UseHalfLambert         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_UseHalfLambert)
#define _CELLThreshold          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_CELLThreshold)
#define _RampMapUOffset         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RampMapUOffset)
#define _RampMapVOffset         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RampMapVOffset)
#define _StylizedSpecularSize       UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_UseHalfLambert)
#define _StylizedSpecularSoftness   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_StylizedSpecularSoftness)
#define _StylizedSpecularAlbedoWeight   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_StylizedSpecularAlbedoWeight)
#define _Shininess   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Shininess)
#define _RimDirectionLightContribution              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimDirectionLightContribution)
#define _RimThreshold              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimThreshold)
#define _RimSoftness              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_RimSoftness)
#define _ScreenSpaceRimWidth              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimWidth)
#define _ScreenSpaceRimThreshold              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimThreshold)
#define _ScreenSpaceRimSoftness              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ScreenSpaceRimSoftness)
#define _AnisoShiftScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_AnisoShiftScale)
#define _ansioSpeularShift              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularShift)
#define _ansioSecondarySpeularShift              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularShift)
#define _ansioSpeularStrength              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularStrength)
#define _ansioSecondarySpeularStrength              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularStrength)
#define _ansioSpeularExponent              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSpeularExponent)
#define _ansioSecondarySpeularExponent              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ansioSecondarySpeularExponent)
#define _anisoSpread1              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_anisoSpread1)
#define _anisoSpread2              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_anisoSpread2)
#define _Parallax               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Parallax)
#define _OcclusionStrength      UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_OcclusionStrength)
#define _ClearCoatMask          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ClearCoatMask)
#define _ClearCoatSmoothness    UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_ClearCoatSmoothness)
#define _DetailAlbedoMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_DetailAlbedoMapScale)
#define _DetailNormalMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_DetailNormalMapScale)
#define _Surface                UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Surface)
#endif

TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);
TEXTURE2D(_PBRLightMap);       SAMPLER(sampler_PBRLightMap);
TEXTURE2D(_AnisoShiftMap);       SAMPLER(sampler_AnisoShiftMap);


#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SamplePBRLightMap(float2 uv, TEXTURE2D_PARAM(pbrLightMap, samplerpbrLightMap))
{
    return half4(SAMPLE_TEXTURE2D(pbrLightMap, samplerpbrLightMap, uv));
}

inline void InitializeNPRStandardSurfaceData(float2 uv, out NPRSurfaceData outSurfaceData)
{
    outSurfaceData = (NPRSurfaceData)0;
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half4 pbrLightMap = SamplePBRLightMap(uv, TEXTURE2D_ARGS(_PBRLightMap, sampler_PBRLightMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.smoothness = _Smoothness * pbrLightMap.a;
    outSurfaceData.metallic = _Metallic * pbrLightMap.r;
    outSurfaceData.occlusion = LerpWhiteTo(pbrLightMap.g, _OcclusionStrength);
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

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
