#ifndef UNIVERSAL_NPR_SURFACE_DATA_INCLUDED
#define UNIVERSAL_NPR_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct NPRSurfaceData
{
    half3 albedo;
    half3 specular;
    half3 normalTS;
    half3 emission;
    
    half  metallic;
    half  smoothness;
    half  occlusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;
    half  specularIntensity;
    half  diffuseID;
    half  innerLine;
    
    #if EYE
        half3 corneaNormalData;
        half3 irisNormalData;
        half  parallax;
    #endif
};

#endif
