#ifndef UNIVERSAL_NPR_UTILS_INCLUDED
#define UNIVERSAL_NPR_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

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
