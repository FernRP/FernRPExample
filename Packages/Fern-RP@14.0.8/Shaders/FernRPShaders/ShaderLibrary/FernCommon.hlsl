#ifndef UNIVERSAL_FERNCOMMON_INCLUDED
#define UNIVERSAL_FERNCOMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "../ShaderLibrary/DeclareDepthShadowTexture.hlsl"
#include "FernShaderUtils.hlsl"

#if _NPR
#include "../ShaderLibrary/NPRSurfaceData.hlsl"
#elif _MIRCOGARIN 
#include "../ShaderLibrary/MicroGarinSurfaceData.hlsl"
#endif

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
//                          Lighting Data                                    //
///////////////////////////////////////////////////////////////////////////////
struct LightingData
{
    half3 lightColor;
    half3 HalfDir;
    half3 lightDir;
    half NdotL;
    half NdotLClamp;
    half HalfLambert;
    half NdotVClamp;
    half NdotHClamp;
    half LdotHClamp;
    half VdotHClamp;
    half ShadowAttenuation;
};


///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////
inline void NPRMainLightCorrect(half lightDirectionObliqueWeight, inout Light mainLight)
{
    mainLight.direction.y = lerp(mainLight.direction.y, 0, lightDirectionObliqueWeight);
    mainLight.direction = normalize(mainLight.direction);
}

half LightingRadiance(LightingData lightingData, half useHalfLambert, half occlusion, half useRadianceOcclusion)
{
    half radiance = lerp(lightingData.NdotLClamp, lightingData.HalfLambert, useHalfLambert);
    radiance = saturate(lerp(radiance, (radiance + occlusion) * 0.5, useRadianceOcclusion)) * lightingData.ShadowAttenuation;
    return radiance;
}

half LightingRadiance(LightingData lightingData)
{
    half radiance = lightingData.NdotLClamp;
    return radiance;
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
