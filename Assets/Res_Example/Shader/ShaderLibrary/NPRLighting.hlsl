#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
    half3 lightColor;
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
    lightData.lightColor = mainLight.color;
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

inline half3 RampShadingDiffuse(half radiance, half rampVOffset, half uOffset, TEXTURE2D_PARAM(rampMap, sampler_rampMap))
{
    half3 diffuse = 0;
    float2 uv = float2(saturate(radiance + uOffset), rampVOffset);
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

half3 StylizedSpecular(half3 albedo, half ndothClamp, half specularSize, half specularSoftness, half albedoWeight)
{
    half specSize = 1 - (specularSize * specularSize);
    half ndothStylized = (ndothClamp - specSize * specSize) / (1 - specSize);
    half specular = LinearStep(0, specularSoftness, ndothStylized);
    specular = lerp(specular, albedo * specular, albedoWeight);
    return specular;
}

half BlinnPhongSpecular(half shininess, half ndoth)
{
    half phongSmoothness = exp2(10 * shininess + 1);
    half normalize = (phongSmoothness + 7) * INV_PI8; // bling-phong 能量守恒系数
    half specular = max(pow(ndoth, phongSmoothness) * normalize, 0.001);
    return specular;
}

struct AnisoSpecularData
{
    half3 specularColor;
    half3 specularSecondaryColor;
    half specularShift;
    half specularSecondaryShift;
    half specularStrength;
    half specularSecondaryStrength;
    half specularExponent;
    half specularSecondaryExponent;
    half spread1;
    half spread2;
};
    
inline half3 AnisotropyDoubleSpecular(BRDFData brdfData, half2 uv, half4 tangentWS, InputData inputData, LightingData lightingData,
    AnisoSpecularData anisoSpecularData, TEXTURE2D_PARAM(anisoDetailMap, sampler_anisoDetailMap))
{
    half4 specMask = 1; // TODO ADD Mask
    half4 detailNormal = SAMPLE_TEXTURE2D(anisoDetailMap,sampler_anisoDetailMap, uv);

    float2 jitter =(detailNormal.y-0.5) * float2(anisoSpecularData.spread1,anisoSpecularData.spread2);

    float sgn = tangentWS.w;
    float3 T = normalize(sgn * cross(inputData.normalWS.xyz, tangentWS.xyz));

    float3 t1 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularShift + jitter.x);
    float3 t2 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularSecondaryShift + jitter.y);

    float3 hairSpec1 = anisoSpecularData.specularColor * anisoSpecularData.specularStrength *
        D_KajiyaKay(t1, lightingData.HalfDir, anisoSpecularData.specularExponent);
    float3 hairSpec2 = anisoSpecularData.specularSecondaryColor * anisoSpecularData.specularSecondaryStrength *
        D_KajiyaKay(t2, lightingData.HalfDir, anisoSpecularData.specularSecondaryExponent);

    float3 F = F_Schlick(half3(0.2,0.2,0.2), lightingData.LdotHClamp);
    half3 anisoSpecularColor = 0.25 * F * (hairSpec1 + hairSpec2) * lightingData.NdotLClamp * specMask * brdfData.specular;
    return anisoSpecularColor;
}

half3 NPRGlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);

    #if defined(UNITY_USE_NATIVE_HDR)
    half3 irradiance = encodedIrradiance.rgb;
    #else
    half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    #endif

    return irradiance * occlusion;
    #endif // GLOSSY_REFLECTIONS

    return _GlossyEnvironmentColor.rgb * occlusion;
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


///////////////////////////////////////////////////////////////////////////////
//                                深度边缘                                    //
///////////////////////////////////////////////////////////////////////////////
half DepthNormal(half depth)
{
    half near = _ProjectionParams.y;
    half far = _ProjectionParams.z;
	
    #if UNITY_REVERSED_Z
    depth = 1.0 - depth;
    #endif
	
    half ortho = (far - near) * depth + near;
    return lerp(depth, ortho, unity_OrthoParams.w);
}

static float2 SamplePoint[9] = 
{
    float2(-1,1), float2(0,1), float2(1,1),
    float2(-1,0), float2(1,0), float2(-1,-1),
    float2(0,-1), float2(1,-1), float2(0, 0)
};

half SobelDepth(half ldc, half ldl, half ldr, half ldu, half ldd)
{
    return ((ldl - ldc) +
        (ldr - ldc) +
        (ldu - ldc) +
        (ldd - ldc)) * 0.25f;
}

half SobelSampleDepth(half2 uv, half2 offset)
{
    //half pixelCenter = thisDepthZ;
    half pixelCenter = LinearEyeDepth(SampleSceneDepth(uv).r, _ZBufferParams);
    half pixelLeft = LinearEyeDepth(SampleSceneDepth( uv + offset.xy * SamplePoint[1]).r, _ZBufferParams);
    half pixelRight = LinearEyeDepth(SampleSceneDepth(uv + offset.xy * SamplePoint[3]).r, _ZBufferParams);
    half pixelUp = LinearEyeDepth(SampleSceneDepth(uv + offset.xy * SamplePoint[4]).r, _ZBufferParams);
    half pixelDown = LinearEyeDepth(SampleSceneDepth(uv + offset.xy * SamplePoint[6]).r, _ZBufferParams);

    return SobelDepth(pixelCenter, pixelLeft, pixelRight, pixelUp, pixelDown);
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
