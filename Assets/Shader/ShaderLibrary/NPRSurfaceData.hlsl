#ifndef UNIVERSAL_NPR_SURFACE_DATA_INCLUDED
#define UNIVERSAL_NPR_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct NPRSurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;
};

#endif
