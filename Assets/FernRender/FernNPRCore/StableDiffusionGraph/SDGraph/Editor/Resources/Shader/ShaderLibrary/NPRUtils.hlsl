#ifndef UNIVERSAL_NPR_UTILS_INCLUDED
#define UNIVERSAL_NPR_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                         Utils Function                                    //
///////////////////////////////////////////////////////////////////////////////

#define EPSILON 5.960464478e-8


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

float4 CalculateClipPosition(float4 positionCS, float viewSpaceZOffsetAmount)
{
    // Create a copy of the original position
    float4 adjustedPositionCS = positionCS;
    // Calculate the offset in clip space
    float zOffsetCS = viewSpaceZOffsetAmount / (_ProjectionParams.z - _ProjectionParams.y);

    // If using an orthographic camera, adjust the z position accordingly
    if (unity_OrthoParams.w)
    {
        // Determine whether to add or subtract the offset based on the value of UNITY_NEAR_CLIP_VALUE
        zOffsetCS *= sign(UNITY_NEAR_CLIP_VALUE);
        // Add the z offset to the adjusted position
        adjustedPositionCS.z += zOffsetCS;
    }
    // If using a perspective camera, adjust the z position accordingly
    else
    {
        // Calculate the modified position in view space
        float modifiedPositionVS_Z = -max(_ProjectionParams.y + EPSILON, abs(positionCS.w) - viewSpaceZOffsetAmount);
        // Convert the modified position to clip space
        float modifiedPositionCS_Z = modifiedPositionVS_Z * UNITY_MATRIX_P[2].z + UNITY_MATRIX_P[2].w;
        // Adjust the z position of the adjusted position
        adjustedPositionCS.z = modifiedPositionCS_Z * positionCS.w / (-modifiedPositionVS_Z);
    }

    // Return the adjusted position
    return adjustedPositionCS;
}

#endif
