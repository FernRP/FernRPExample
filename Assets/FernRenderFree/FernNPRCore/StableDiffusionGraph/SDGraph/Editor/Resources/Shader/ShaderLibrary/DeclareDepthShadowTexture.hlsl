#ifndef UNITY_DECLARE_DEPTHSHADOW_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTHSHADOW_TEXTURE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthShadowTexture);
SAMPLER(sampler_CameraDepthShadowTexture);

float4 _CameraDepthShadowTexture_TexelSize;

float SampleSceneDepthShadow(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraDepthShadowTexture, sampler_CameraDepthShadowTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

float LoadSceneDepthShadow(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraDepthShadowTexture, uv).r;
}
#endif
