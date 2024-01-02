#ifndef UNIVERSAL_FERN_SURFACE_DATA_INCLUDED
#define UNIVERSAL_FERN_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct FernSurfaceData
{
    half3 albedo;
    half3 specular;
    half3 normalTS;
    half3 emission;
    
    half  metallic;
    half  smoothness;
    half  occlusion;
    half  alpha;
    half  diffuseID;
    half  innerLine;
    
    half3 porousColor;
    half  porousDensity;
    half  porousSmoothness;
    half  porousMetallic;
};

#endif //UNIVERSAL_FERN_SURFACE_DATA_INCLUDED
