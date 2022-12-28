#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/NPRBSDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "../ShaderLibrary/DeclareDepthShadowTexture.hlsl"
#include "../ShaderLibrary/NPRSurfaceData.hlsl"
#include "../ShaderLibrary/NPRUtils.hlsl"

#define PI8 25.1327
#define INV_PI8 0.039789

#if defined(LIGHTMAP_ON)
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
    #define OUTPUT_SH(normalWS, OUT)
#else
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
    #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif

#if FACE
CBUFFER_START(SDFFaceObjectToWorld)
    float4x4 _FaceObjectToWorld;
CBUFFER_END
#endif

float4 _CameraDepthTexture_TexelSize;


///////////////////////////////////////////////////////////////////////////////
//                          Lighting Data                                    //
///////////////////////////////////////////////////////////////////////////////

/**
 * \brief DepthUV For Rim Or Shadow
 * \param offset 
 * \param reverseX usually use directional light's dir, but sometime need x reverse
 * \param positionCSXY 
 * \param mainLightDir 
 * \param depthTexWH 
 * \param addInputData 
 * \return 
 */
inline int2 GetDepthUVOffset(half offset, half reverseX, half2 positionCSXY, half3 mainLightDir, half2 depthTexWH, NPRAddInputData addInputData)
{
    // 1 / depth when depth < 1 is wrong, this is like point light attenuation
    // 0.5625 is aspect, hard code for now
    // 0.333 is fov, hard code for now
    float2 UVOffset = 0.5625f * (offset * 0.333f / (1 + addInputData.linearEyeDepth)); 
    half2 mainLightDirVS = TransformWorldToView(mainLightDir).xy;
    mainLightDirVS.x *= lerp(1, -1, reverseX);
    UVOffset = mainLightDirVS * UVOffset;
    half2 downSampleFix = _CameraDepthTexture_TexelSize.zw / depthTexWH.xy;
    int2 loadTexPos = positionCSXY / downSampleFix + UVOffset * depthTexWH.xy;
    loadTexPos = min(loadTexPos, depthTexWH.xy-1);
    return loadTexPos;
}

inline half DepthShadow(half depthShadowOffset, half reverseX, half depthShadowThresoldOffset, half depthShadowSoftness, half2 positionCSXY, half3 mainLightDir, NPRAddInputData addInputData)
{
    int2 loadPos = GetDepthUVOffset(depthShadowOffset, reverseX, positionCSXY, mainLightDir, _CameraDepthShadowTexture_TexelSize.zw, addInputData);
    float depthShadowTextureValue = LoadSceneDepthShadow(loadPos);
    float depthTextureLinearDepth = DepthSamplerToLinearDepth(depthShadowTextureValue);
    float depthTexShadowDepthDiffThreshold = 0.025f + depthShadowThresoldOffset;

    half depthShadow = saturate((depthTextureLinearDepth - (addInputData.linearEyeDepth - depthTexShadowDepthDiffThreshold)) * 50 / depthShadowSoftness);
    return depthShadow;
}

inline half DepthRim(half depthRimOffset, half reverseX, half rimDepthDiffThresholdOffset, half2 positionCSXY, half3 mainLightDir, NPRAddInputData addInputData)
{
    int2 loadPos = GetDepthUVOffset(depthRimOffset, reverseX, positionCSXY, mainLightDir,  _CameraDepthTexture_TexelSize.zw, addInputData);
    float depthTextureValue = LoadSceneDepth(loadPos);
    float depthTextureLinearDepth = DepthSamplerToLinearDepth(depthTextureValue);
    
    float threshold = saturate(0.1 + rimDepthDiffThresholdOffset);
    half depthRim = saturate((depthTextureLinearDepth - (addInputData.linearEyeDepth + threshold)) * 5);
    depthRim = lerp(0, depthRim, addInputData.linearEyeDepth);
    return depthRim;
}

struct LightingData
{
    half3 lightColor;
    half3 HalfDir;
    half3 lightDir;
    half NdotL;
    half NdotLClamp;
    half HalfLambert;
    half NdotVClamp;
    half NdotHClamp;
    half LdotHClamp;
    half VdotHClamp;
    half ShadowAttenuation;
};


///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////

#if FACE
inline void SDFFaceUV(half reversal, half faceArea, out half2 result)
    {
        Light mainLight = GetMainLight();
        half2 lightDir = normalize(mainLight.direction.xz);

        half2 Front = normalize(_FaceObjectToWorld._13_33);
        half2 Right = normalize(_FaceObjectToWorld._11_31);

        float FdotL = dot(Front, lightDir);
        float RdotL = dot(Right, lightDir) * lerp(1, -1, reversal);
        result.x = 1 - max(0,-(acos(FdotL) * INV_PI * 90.0 /(faceArea+90.0) -0.5) * 2);
        result.y = 1 - 2 * step(RdotL, 0);
    }

    inline half3 SDFFaceDiffuse(half4 uv, LightingData lightData, half SDFShadingSoftness, half3 highColor, half3 darkColor, TEXTURE2D_X_PARAM(_SDFFaceTex, sampler_SDFFaceTex))
    {
        half FdotL = uv.z;
        half sign = uv.w;
        half SDFMap = SAMPLE_TEXTURE2D(_SDFFaceTex, sampler_SDFFaceTex, uv.xy * float2(-sign, 1)).r;
        //half diffuseRadiance = saturate((abs(FdotL) - SDFMap) * SDFShadingSoftness * 500);
        half diffuseRadiance = smoothstep(-SDFShadingSoftness * 0.1, SDFShadingSoftness * 0.1, (abs(FdotL) - SDFMap)) * lightData.ShadowAttenuation;
        half3 diffuseColor = lerp(darkColor.rgb, highColor.rgb, diffuseRadiance);
        return diffuseColor;
    }
    #endif

inline void NPRMainLightCorrect(half lightDirectionObliqueWeight, inout Light mainLight)
{
    #if FACE
        mainLight.direction.y = lerp(mainLight.direction.y, 0, lightDirectionObliqueWeight);
        mainLight.direction = normalize(mainLight.direction);
    #endif
}

half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
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

half LightingRadiance(LightingData lightingData, half useHalfLambert, half occlusion, half useRadianceOcclusion)
{
    half radiance = lerp(lightingData.NdotLClamp, lightingData.HalfLambert, useHalfLambert);
    radiance = saturate(lerp(radiance, (radiance + occlusion) * 0.5, useRadianceOcclusion)) * lightingData.ShadowAttenuation;
    return radiance;
}

/**
 * \brief Get Cell Shading Radiance
 * \param radiance 
 * \param shadowThreshold 
 * \param shadowSmooth 
 * \param diffuse [Out]
 */
inline half3 CellShadingDiffuse(inout half radiance, half cellThreshold, half cellSmooth, half3 highColor, half3 darkColor)
{
    half3 diffuse = 0;
    //cellSmooth *= 0.5;
    radiance = saturate(1 + (radiance - cellThreshold - cellSmooth) / max(cellSmooth, 1e-3));
    // 0.5 cellThreshold 0.5 smooth = Lambert
    //radiance = LinearStep(cellThreshold - cellSmooth, cellThreshold + cellSmooth, radiance);
    diffuse = lerp(darkColor.rgb, highColor.rgb, radiance);
    return diffuse;
}

inline half3 CellBandsShadingDiffuse(inout half radiance, half cellThreshold, half cellBandSoftness, half cellBands, half3 highColor, half3 darkColor)
{
    half3 diffuse = 0;
    //cellSmooth *= 0.5;
    radiance = saturate(1 + (radiance - cellThreshold - cellBandSoftness) / max(cellBandSoftness, 1e-3));
    // 0.5 cellThreshold 0.5 smooth = Lambert
    //radiance = LinearStep(cellThreshold - cellSmooth, cellThreshold + cellSmooth, radiance);

    #if _CELLBANDSHADING
        half bandsSmooth = cellBandSoftness;
        radiance = saturate((LinearStep(0.5 - bandsSmooth, 0.5 + bandsSmooth, frac(radiance * cellBands)) + floor(radiance * cellBands)) / cellBands);
    #endif

    diffuse = lerp(darkColor.rgb, highColor.rgb, radiance);
    return diffuse;
}

inline half3 RampShadingDiffuse(half radiance, half rampVOffset, half uOffset, TEXTURE2D_PARAM(rampMap, sampler_rampMap))
{
    half3 diffuse = 0;
    float2 uv = float2(saturate(radiance + uOffset), rampVOffset);
    diffuse = SAMPLE_TEXTURE2D(rampMap, sampler_rampMap, uv).rgb;
    return diffuse;
}

half GGXDirectBRDFSpecular(BRDFData brdfData, half3 LoH, half3 NoH)
{
    float d = NoH.x * NoH.x * brdfData.roughness2MinusOne + 1.00001f;
    half LoH2 = LoH.x * LoH.x;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    #if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
    specularTerm = specularTerm - HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
    #endif

    return specularTerm;
}

half3 StylizedSpecular(half3 albedo, half ndothClamp, half specularSize, half specularSoftness, half albedoWeight)
{
    half specSize = 1 - (specularSize * specularSize);
    half ndothStylized = (ndothClamp - specSize * specSize) / (1 - specSize);
    half3 specular = LinearStep(0, specularSoftness, ndothStylized);
    specular = lerp(specular, albedo * specular, albedoWeight);
    return specular;
}

half BlinnPhongSpecular(half shininess, half ndoth)
{
    half phongSmoothness = exp2(10 * shininess + 1);
    half normalize = (phongSmoothness + 7) * INV_PI8; // bling-phong 能量守恒系数
    half specular = max(pow(ndoth, phongSmoothness) * normalize, 0.001);
    return specular;
}

inline half3 AnisotropyDoubleSpecular(BRDFData brdfData, half2 uv, half4 tangentWS, InputData inputData, LightingData lightingData,
    AnisoSpecularData anisoSpecularData, TEXTURE2D_PARAM(anisoDetailMap, sampler_anisoDetailMap))
{
    half specMask = 1; // TODO ADD Mask
    half4 detailNormal = SAMPLE_TEXTURE2D(anisoDetailMap,sampler_anisoDetailMap, uv);

    float2 jitter =(detailNormal.y-0.5) * float2(anisoSpecularData.spread1,anisoSpecularData.spread2);

    float sgn = tangentWS.w;
    float3 T = normalize(sgn * cross(inputData.normalWS.xyz, tangentWS.xyz));
    //float3 T = normalize(tangentWS.xyz);

    float3 t1 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularShift + jitter.x);
    float3 t2 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularSecondaryShift + jitter.y);

    float3 hairSpec1 = anisoSpecularData.specularColor * anisoSpecularData.specularStrength *
        D_KajiyaKay(t1, lightingData.HalfDir, anisoSpecularData.specularExponent);
    float3 hairSpec2 = anisoSpecularData.specularSecondaryColor * anisoSpecularData.specularSecondaryStrength *
        D_KajiyaKay(t2, lightingData.HalfDir, anisoSpecularData.specularSecondaryExponent);

    float3 F = F_Schlick(half3(0.2,0.2,0.2), lightingData.LdotHClamp);
    half3 anisoSpecularColor = 0.25 * F * (hairSpec1 + hairSpec2) * lightingData.NdotLClamp * specMask * brdfData.specular;
    return anisoSpecularColor;
}

inline half3 AngleRingSpecular(AngleRingSpecularData specularData, InputData inputData, half radiance, LightingData lightingData)
{
    half3 specularColor = 0;
    half mask = specularData.mask;
    float3 normalV = mul(UNITY_MATRIX_V, half4(inputData.normalWS, 0)).xyz;
    float3 halfV = mul(UNITY_MATRIX_V, half4(lightingData.HalfDir, 0)).xyz;
    half ndh = dot(normalize(normalV.xz), normalize(halfV.xz));

    ndh = pow(ndh, 6) * specularData.width * radiance;

    half lightFeather = specularData.softness * ndh;

    half lightStepMax = saturate(1 - ndh + lightFeather);
    half lightStepMin = saturate(1 - ndh - lightFeather);

    half brightArea = LinearStep(lightStepMin, lightStepMax, min(mask, 0.99));
    half3 lightColor_B = brightArea * specularData.brightColor;
    half3 lightColor_S = LinearStep(specularData.threshold, 1, mask) * specularData.shadowColor;
    specularColor = (lightColor_S + lightColor_B) * specularData.intensity;
    return specularColor;
}

half3 NPRGlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);

    #if defined(UNITY_USE_NATIVE_HDR)
    half3 irradiance = encodedIrradiance.rgb;
    #else
    half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    #endif

    return irradiance * occlusion;
    #endif // GLOSSY_REFLECTIONS

    return _GlossyEnvironmentColor.rgb * occlusion;
}


///////////////////////////////////////////////////////////////////////////////
//                         Depth Screen Space                                //
///////////////////////////////////////////////////////////////////////////////
half DepthNormal(half depth)
{
    half near = _ProjectionParams.y;
    half far = _ProjectionParams.z;
	
    #if UNITY_REVERSED_Z
    depth = 1.0 - depth;
    #endif
	
    half ortho = (far - near) * depth + near;
    return lerp(depth, ortho, unity_OrthoParams.w);
}

inline half3 SamplerMatCap(half4 matCapColor, half2 uv, half3 normalWS, half2 screenUV, TEXTURE2D_PARAM(matCapTex, sampler_matCapTex))
{
    half3 finalMatCapColor = 0;
    #if _MATCAP
        #if _NORMALMAP
            half3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
            half2 matcapUV = normalVS.xy * 0.5 + 0.5;
        #else
            half2 matcapUV = uv;
        #endif
        half3 matCap = SAMPLE_TEXTURE2D(matCapTex, sampler_matCapTex, matcapUV).xyz;
        finalMatCapColor = matCap.xyz * matCapColor.rgb;
    #endif
    return finalMatCapColor;
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
