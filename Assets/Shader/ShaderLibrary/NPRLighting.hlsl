#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

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
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////

half LightingRadiance(half3 lightDir, half3 normal, half useHalfLambert)
{
    half NdotL = saturate(dot(normal, lightDir));
    NdotL = lerp(NdotL, NdotL * 0.5 + 0.5, useHalfLambert);
    return NdotL;
}

/**
 * \brief Get Cell Shading Radiance
 * \param radiance 
 * \param shadowThreshold 
 * \param shadowSmooth 
 * \param diffuse [Out]
 */
inline void StylizedDiffuse(inout half radiance, half cellhreshold, half cellSmooth, half3 highColor, half3 darkColor, out half3 diffuse)
{
    cellSmooth *= 0.5;
    radiance = saturate(1 + (radiance - cellhreshold - cellSmooth) / max(cellSmooth, 1e-3));
    diffuse = lerp(darkColor.rgb, highColor.rgb, radiance);
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
