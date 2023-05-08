#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#if (VERSION_LOWER(13, 0))
uint GetMeshRenderingLayer()
{
    return DEFAULT_LIGHT_LAYERS;
}

half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness, half2 normalizedScreenSpaceUV)
{
    return CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness);
}
#endif
