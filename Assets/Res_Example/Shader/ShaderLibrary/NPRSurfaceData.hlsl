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

struct AngleRingSpecularData
{
    half3 shadowColor;
    half3 brightColor;
    half mask;
    half width;
    half softness;
    half threshold;
    half intensity;
};

#endif
