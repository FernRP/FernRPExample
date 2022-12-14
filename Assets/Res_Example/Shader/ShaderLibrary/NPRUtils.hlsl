#ifndef UNIVERSAL_NPR_UTILS_INCLUDED
#define UNIVERSAL_NPR_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                         Utils Function                                    //
///////////////////////////////////////////////////////////////////////////////

half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In-minValue) / (maxValue - minValue));
}

static float2 SamplePoint[9] = 
{
    float2(-1,1), float2(0,1), float2(1,1),
    float2(-1,0), float2(1,0), float2(-1,-1),
    float2(0,-1), float2(1,-1), float2(0, 0)
};

half Sobel(half ldc, half ldl, half ldr, half ldu, half ldd)
{
    return ((ldl - ldc) +
        (ldr - ldc) +
        (ldu - ldc) +
        (ldd - ldc)) * 0.25f;
}

float DepthSamplerToLinearDepth(float positionCSZ)
{
    if(unity_OrthoParams.w)
    {
        #if defined(UNITY_REVERSED_Z)
        positionCSZ = UNITY_NEAR_CLIP_VALUE == 1 ? 1-positionCSZ : positionCSZ;
        #endif

        return lerp(_ProjectionParams.y, _ProjectionParams.z, positionCSZ);
    }else {
        return LinearEyeDepth(positionCSZ,_ZBufferParams);
    }
}

#endif
