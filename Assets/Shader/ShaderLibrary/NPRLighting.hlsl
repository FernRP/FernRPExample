#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#define PI8 25.1327
#define INV_PI8 0.039789

#if defined(LIGHTMAP_ON)
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
    #define OUTPUT_SH(normalWS, OUT)
#else
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
    #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif

///////////////////////////////////////////////////////////////////////////////
//                         Utils Function                                    //
///////////////////////////////////////////////////////////////////////////////

half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In-minValue) / (maxValue - minValue));
}

///////////////////////////////////////////////////////////////////////////////
//                          Lighting Data                                    //
///////////////////////////////////////////////////////////////////////////////
struct LightingData
{
    half3 HalfDir;
    half NdotL;
    half NdotLClamp;
    half HalfLambert;
    half NdotVClamp;
    half NdotHClamp;
    half LdotHClamp;
    half VdotHClamp;
    half ShadowAttenuation;
};

LightingData InitializeLightingData(Light mainLight, half3 normalWS, half3 viewDirectionWS)
{
    LightingData lightData;
    lightData.NdotL = dot(normalWS, mainLight.direction.xyz);
    lightData.NdotLClamp = saturate(lightData.NdotL);
    lightData.HalfLambert = lightData.NdotL * 0.5 + 0.5;
    half3 halfDir = SafeNormalize(mainLight.direction + viewDirectionWS);
    lightData.LdotHClamp = saturate(dot(mainLight.direction.xyz, halfDir.xyz));
    lightData.NdotHClamp = saturate(dot(normalWS.xyz, halfDir.xyz));
    lightData.NdotVClamp = saturate(dot(normalWS.xyz, viewDirectionWS.xyz));
    lightData.HalfDir = halfDir;
    #if defined(_RECEIVE_SHADOWS_OFF)
    inputDotData.ShadowAttenuation = 1;
    #else
    lightData.ShadowAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    #endif
    return lightData;
}

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////

half LightingRadiance(LightingData lightingData, half useHalfLambert)
{
    half radiance = lerp(lightingData.NdotLClamp, lightingData.HalfLambert, useHalfLambert) * lightingData.ShadowAttenuation;
    return radiance;
}

/**
 * \brief Get Cell Shading Radiance
 * \param radiance 
 * \param shadowThreshold 
 * \param shadowSmooth 
 * \param diffuse [Out]
 */
inline half3 CellShadingDiffuse(inout half radiance, half cellThreshold, half cellSmooth, half3 highColor, half3 darkColor)
{
    half3 diffuse = 0;
    //cellSmooth *= 0.5;
    radiance = saturate(1 + (radiance - cellThreshold - cellSmooth) / max(cellSmooth, 1e-3));
    // 0.5 cellThreshold 0.5 smooth = Lambert
    //radiance = LinearStep(cellThreshold - cellSmooth, cellThreshold + cellSmooth, radiance);
    diffuse = lerp(darkColor.rgb, highColor.rgb, radiance);
    return diffuse;
}


inline half3 RampShadingDiffuse(half radiance, half rampVOffset, TEXTURE2D_PARAM(rampMap, sampler_rampMap))
{
    half3 diffuse = 0;
    float2 uv = float2(saturate(radiance + _RampMapUOffset), rampVOffset);
    diffuse = SAMPLE_TEXTURE2D(rampMap, sampler_rampMap, uv).rgb;
    return diffuse;
}


half GGXDirectBRDFSpecular(BRDFData brdfData, half3 LoH, half3 NoH)
{
    float d = NoH.x * NoH.x * brdfData.roughness2MinusOne + 1.00001f;
    half LoH2 = LoH.x * LoH.x;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    #if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
    specularTerm = specularTerm - HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
    #endif

    return specularTerm;
}

half3 StylizedSpecular(half3 albedo, half ndothClamp)
{
    half specSize = 1 - (_StylizedSpecularSize * _StylizedSpecularSize);
    half ndothStylized = (ndothClamp - specSize * specSize) / (1 - specSize);
    half specularSoftness = _StylizedSpecularSoftness;
    half specular = LinearStep(0, specularSoftness, ndothStylized);
    specular = lerp(specular, albedo * specular, _StylizedSpecularAlbedoWeight);
    return specular;
}

half BlinnPhongSpecular(half shininess, half ndoth)
{
    half phongSmoothness = exp2(10 * shininess + 1);
    half normalize = (phongSmoothness + 7) * INV_PI8; // bling-phong 能量守恒系数
    half specular = max(pow(ndoth, phongSmoothness) * normalize, 0.001);
    return specular;
}

half3 VertexLighting(float3 positionWS, half3 normalWS)
{
    half3 vertexLightColor = half3(0.0, 0.0, 0.0);

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    uint lightsCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(lightsCount)
        Light light = GetAdditionalLight(lightIndex, positionWS);
    half3 lightColor = light.color * light.distanceAttenuation;
    vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
    LIGHT_LOOP_END
#endif

    return vertexLightColor;
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
