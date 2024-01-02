#ifndef UNIVERSAL_NPRLIGHTING_INCLUDED
#define UNIVERSAL_NPRLIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "../ShaderLibrary/DeclareDepthShadowTexture.hlsl"
#include "FernShaderUtils.hlsl"
#include "../ShaderLibrary/NPRBSDF.hlsl"

#if FACE
CBUFFER_START(SDFFaceObjectToWorld)
    float4x4 _FaceObjectToWorld;
CBUFFER_END
#endif

// Global Property
half4 _DepthTextureSourceSize;
half _CameraAspect;
half _CameraFOV;

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
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
inline uint2 GetDepthUVOffset(half offset, half reverseX, half2 positionCSXY, half3 mainLightDir, half2 depthTexWH, FernAddInputData addInputData)
{
    // 1 / depth when depth < 1 is wrong, this is like point light attenuation
    // 0.5625 is aspect, hard code for now
    // 0.333 is fov, hard code for now
    float2 UVOffset = _CameraAspect * (offset * 2 * _CameraFOV / (1 + addInputData.linearEyeDepth)); 
    half2 mainLightDirVS = TransformWorldToViewDir(mainLightDir, true).xz;
    mainLightDirVS.x *= lerp(1, -1, reverseX);
    UVOffset = mainLightDirVS * UVOffset;
    half2 downSampleFix = _DepthTextureSourceSize.zw / depthTexWH.xy;
    uint2 loadTexPos = positionCSXY / downSampleFix + UVOffset * depthTexWH.xy;
    loadTexPos = min(loadTexPos, depthTexWH.xy-1);
    return loadTexPos;
}

inline half DepthShadow(half depthShadowOffset, half reverseX, half depthShadowThresoldOffset, half depthShadowSoftness, half2 positionCSXY, half3 mainLightDir, FernAddInputData addInputData)
{
    uint2 loadPos = GetDepthUVOffset(depthShadowOffset, reverseX, positionCSXY, mainLightDir, _CameraDepthShadowTexture_TexelSize.zw, addInputData);
    float depthShadowTextureValue = LoadSceneDepthShadow(loadPos);
    float depthTextureLinearDepth = DepthSamplerToLinearDepth(depthShadowTextureValue);

    float depthTexShadowDepthDiffThreshold = 0.025f + depthShadowThresoldOffset;
    half depthShadow = saturate((depthTextureLinearDepth - (addInputData.linearEyeDepth - depthTexShadowDepthDiffThreshold)) * 50 / depthShadowSoftness);

    return depthShadow;
}

inline half DepthRim(half depthRimOffset, half reverseX, half rimDepthDiffThresholdOffset, half2 positionCSXY, half3 mainLightDir, FernAddInputData addInputData)
{
    int2 loadPos = GetDepthUVOffset(depthRimOffset, reverseX, positionCSXY, mainLightDir,  _DepthTextureSourceSize.zw, addInputData);
    float depthTextureValue = LoadSceneDepth(loadPos);
    float depthTextureLinearDepth = DepthSamplerToLinearDepth(depthTextureValue);
    half depthRim = saturate((depthTextureLinearDepth - (addInputData.linearEyeDepth + rimDepthDiffThresholdOffset)));
    depthRim = lerp(0, depthRim, addInputData.linearEyeDepth);
    return depthRim;
}

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
    half normalize = (phongSmoothness + 7) * INV_PI8;
    half specular = max(pow(ndoth, phongSmoothness) * normalize, 1e-4);
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

half3 NPRGlossyEnvironmentReflection(half3 reflectVector, half3 positionWS, half2 normalizedScreenSpaceUV, half perceptualRoughness, half occlusion)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
        half3 irradiance;
    
        #if defined(_REFLECTION_PROBE_BLENDING) || USE_FORWARD_PLUS
            irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
        #else
            #ifdef _REFLECTION_PROBE_BOX_PROJECTION
                reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
            #endif 

            half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
            half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);

            #if defined(UNITY_USE_NATIVE_HDR)
                irradiance = encodedIrradiance.rgb;
            #else
                irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
            #endif
        #endif
        return irradiance * occlusion;
    #else
        return _GlossyEnvironmentColor.rgb * occlusion;
    #endif // GLOSSY_REFLECTIONS
}

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


///////////////////////////////////////////////////////////////////////////////
//                         Shading Function                                  //
///////////////////////////////////////////////////////////////////////////////


void PreInitializeInputData(Varyings input, half facing, out InputData inputData, out FernAddInputData addInputData)
{
    inputData = (InputData)0;
    addInputData = (FernAddInputData)0;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.positionWS = input.positionWS;

    if(facing < 0)
    {
        input.normalWS = -input.normalWS;
    }

    #if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.tangentToWorld = tangentToWorld;
    #endif

    inputData.normalWS = SafeNormalize(input.normalWS);

    inputData.viewDirectionWS = viewDirWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    #else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    #endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
    // interpolator will cause artifact
    float3 positionCS = ComputeNormalizedDeviceCoordinatesWithZ(input.positionWS, UNITY_MATRIX_VP);
    //float3 positionCS = TransformWorldToHClip(input.positionWS);
    addInputData.linearEyeDepth = DepthSamplerToLinearDepth(positionCS.z);
}

void InitializeInputData(Varyings input, half3 normalTS, inout FernAddInputData addInputData, inout InputData inputData)
{
    #if EYE && (defined(_NORMALMAP) || defined(_DETAIL))
        half3 corneaNormalTS = normalTS;
        half3 irisNormalTS = half3(-corneaNormalTS.x, -corneaNormalTS.y, corneaNormalTS.z);
        half3 tempNormal = corneaNormalTS;
        corneaNormalTS = lerp(corneaNormalTS, irisNormalTS, _BumpIrisInvert);
        irisNormalTS = lerp(irisNormalTS, tempNormal, _BumpIrisInvert);
        addInputData.corneaNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(corneaNormalTS, inputData.tangentToWorld));
        addInputData.irisNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(irisNormalTS, inputData.tangentToWorld));
        inputData.normalWS = addInputData.corneaNormalWS;
    #elif (defined(_NORMALMAP) || defined(_DETAIL))
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
        inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    #endif

    #if defined(DYNAMICLIGHTMAP_ON)
        inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    #else
        inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    #endif
}

LightingData InitializeLightingData(Light mainLight, Varyings input, half3 normalWS, half3 viewDirectionWS,
                                    FernAddInputData addInputData)
{
    LightingData lightData;
    lightData.lightColor = mainLight.color;
    #if EYE
    lightData.NdotL = dot(addInputData.irisNormalWS, mainLight.direction.xyz);
    #else
    lightData.NdotL = dot(normalWS, mainLight.direction.xyz);
    #endif
    lightData.NdotLClamp = saturate(lightData.NdotL);
    lightData.HalfLambert = lightData.NdotL * 0.5 + 0.5;
    half3 halfDir = SafeNormalize(mainLight.direction + viewDirectionWS);
    lightData.LdotHClamp = saturate(dot(mainLight.direction.xyz, halfDir.xyz));
    lightData.NdotHClamp = saturate(dot(normalWS.xyz, halfDir.xyz));
    lightData.NdotVClamp = saturate(dot(normalWS.xyz, viewDirectionWS.xyz));
    lightData.HalfDir = halfDir;
    lightData.lightDir = mainLight.direction;
    #if defined(_RECEIVE_SHADOWS_OFF)
    lightData.ShadowAttenuation = 1;
    #elif _DEPTHSHADOW
    lightData.ShadowAttenuation = DepthShadow(_DepthShadowOffset, _DepthOffsetShadowReverseX, _DepthShadowThresoldOffset, _DepthShadowSoftness, input.positionCS.xy, mainLight.direction, addInputData);
    #else
    lightData.ShadowAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    #endif

    return lightData;
}

half3 NPRDiffuseLighting(BRDFData brdfData, half4 uv, LightingData lightingData, half radiance)
{
    half3 diffuse = 0;

    #if _CELLSHADING
        diffuse = CellShadingDiffuse(radiance, _CELLThreshold, _CELLSmoothing, _HighColor.rgb, _DarkColor.rgb);
    #elif _LAMBERTIAN
    diffuse = lerp(_DarkColor.rgb, _HighColor.rgb, radiance);
    #elif _RAMPSHADING
        diffuse = RampShadingDiffuse(radiance, _RampMapVOffset, _RampMapUOffset, TEXTURE2D_ARGS(_DiffuseRampMap, sampler_DiffuseRampMap));
    #elif _CELLBANDSHADING
        diffuse = CellBandsShadingDiffuse(radiance, _CELLThreshold, _CellBandSoftness, _CellBands,  _HighColor.rgb, _DarkColor.rgb);
    #elif _SDFFACE
        diffuse = SDFFaceDiffuse(uv, lightingData, _SDFShadingSoftness, _HighColor.rgb, _DarkColor.rgb, TEXTURECUBE_ARGS(_SDFFaceTex, sampler_SDFFaceTex));
    #endif
    diffuse *= brdfData.diffuse;
    return diffuse;
}

half3 NPRSpecularLighting(BRDFData brdfData, FernSurfaceData surfData, Varyings input, InputData inputData, half3 albedo,
                          half radiance, LightingData lightData)
{
    half3 specular = 0;
    #if _GGX
        specular = GGXDirectBRDFSpecular(brdfData, lightData.LdotHClamp, lightData.NdotHClamp) * surfData.specularIntensity;
    #elif _STYLIZED
        specular = StylizedSpecular(albedo, lightData.NdotHClamp, _StylizedSpecularSize, _StylizedSpecularSoftness, _StylizedSpecularAlbedoWeight) * surfData.specularIntensity;
    #elif _BLINNPHONG
        specular = BlinnPhongSpecular(_Shininess, lightData.NdotHClamp) * surfData.specularIntensity;
    #elif _KAJIYAHAIR
        half2 anisoUV = input.uv.xy * _AnisoShiftScale;
        AnisoSpecularData anisoSpecularData;
        InitAnisoSpecularData(anisoSpecularData);
        specular = AnisotropyDoubleSpecular(brdfData, anisoUV, input.tangentWS, inputData, lightData, anisoSpecularData,
            TEXTURE2D_ARGS(_AnisoShiftMap, sampler_AnisoShiftMap));
    #elif _ANGLERING
        AngleRingSpecularData angleRingSpecularData;
        InitAngleRingSpecularData(surfData.specularIntensity, angleRingSpecularData);
        specular = AngleRingSpecular(angleRingSpecularData, inputData, radiance, lightData);
    #endif
    specular *= _SpecularColor.rgb * radiance * brdfData.specular;
    return specular;
}

TEXTURE2D(iChannel0);				SAMPLER(sampler_iChannel0);
TEXTURE2D(iChannel1);				SAMPLER(sampler_iChannel1);
TEXTURE2D(iChannel2);				SAMPLER(sampler_iChannel2);

/**
 * \brief Main Lighting, consists of NPR and PBR Lighting Equation
 * \param brdfData 
 * \param brdfDataClearCoat 
 * \param input 
 * \param inputData 
 * \param surfData 
 * \param radiance 
 * \param lightData 
 * \return 
 */
half3 FernMainLightDirectLighting(BRDFData brdfData, BRDFData brdfDataClearCoat, Varyings input, InputData inputData,
                                 FernSurfaceData surfData, LightingData lightData)
{
    half radiance = LightingRadiance(lightData, _UseHalfLambert, surfData.occlusion, _UseRadianceOcclusion);

    half3 diffuse = NPRDiffuseLighting(brdfData, input.uv, lightData, radiance);
    half3 specular = NPRSpecularLighting(brdfData, surfData, input, inputData, surfData.albedo, radiance, lightData);
    half3 brdf = (diffuse + specular) * lightData.lightColor;
    #if defined(_CLEARCOAT)
        // Clear coat evaluates the specular a second timw and has some common terms with the base specular.
        // We rely on the compiler to merge these and compute them only once.
        half3 brdfCoat = kDielectricSpec.r * NPRSpecularLighting(brdfDataClearCoat, surfData, input, inputData, surfData.albedo, radiance, lightData);
        // Mix clear coat and base layer using khronos glTF recommended formula
        // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
        // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
        half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
        // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
        // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
        half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);

        brdf = brdf * (1.0 - surfData.clearCoatMask * coatFresnel) + brdfCoat * surfData.clearCoatMask * lightData.lightColor;
    #endif // _CLEARCOAT
   
    return brdf;
}

half3 FernVertexLighting(float3 positionWS, half3 normalWS)
{
    half3 vertexLightColor = half3(0.0, 0.0, 0.0);

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    uint lightsCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(lightsCount)
        Light light = GetAdditionalLight(lightIndex, positionWS);
        half3 lightColor = light.color * light.distanceAttenuation;
        float pureIntencity = max(0.001,(0.299 * lightColor.r + 0.587 * lightColor.g + 0.114 * lightColor.b));
        lightColor = max(0, lerp(lightColor, lerp(0, min(lightColor, lightColor / pureIntencity * _LightIntensityClamp), 1), _Is_Filter_LightColor));
        vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
    LIGHT_LOOP_END
    #endif

    return vertexLightColor;
}

/**
 * \brief AdditionLighting, Lighting Equation base on MainLight, TODO: if cell-shading should use other lighting equation
 * \param brdfData 
 * \param brdfDataClearCoat 
 * \param input 
 * \param inputData 
 * \param surfData 
 * \param addInputData 
 * \param shadowMask 
 * \param meshRenderingLayers 
 * \param aoFactor 
 * \return 
 */
half3 FernAdditionLightDirectLighting(BRDFData brdfData, BRDFData brdfDataClearCoat, Varyings input, InputData inputData,
                                     FernSurfaceData surfData,
                                     FernAddInputData addInputData, half4 shadowMask, half meshRenderingLayers,
                                     AmbientOcclusionFactor aoFactor)
{
    half3 additionLightColor = 0;
    float pureIntensityMax = 0;
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            LightingData lightingData = InitializeLightingData(light, input, inputData.normalWS, inputData.viewDirectionWS, addInputData);
            half radiance = LightingRadiance(lightingData, _UseHalfLambert, surfData.occlusion, _UseRadianceOcclusion);
            // Additional Light Filter Referenced from https://github.com/unity3d-jp/UnityChanToonShaderVer2_Project
            float pureIntencity = 0.299 * lightingData.lightColor.r + 0.587 * lightingData.lightColor.g + 0.114 * lightingData.lightColor.b;
            lightingData.lightColor = max(0, lerp(lightingData.lightColor, min(lightingData.lightColor, lightingData.lightColor / pureIntencity * _LightIntensityClamp), _Is_Filter_LightColor));
            half3 addLightColor = FernMainLightDirectLighting(brdfData, brdfDataClearCoat, input, inputData, surfData, lightingData);
            additionLightColor += addLightColor;
        }
    }
    #endif

    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            LightingData lightingData = InitializeLightingData(light, input, inputData.normalWS, inputData.viewDirectionWS, addInputData);
            half radiance = LightingRadiance(lightingData, _UseHalfLambert, surfData.occlusion, _UseRadianceOcclusion);
            // Additional Light Filter Referenced from https://github.com/unity3d-jp/UnityChanToonShaderVer2_Project
            float pureIntencity = 0.299 * lightingData.lightColor.r + 0.587 * lightingData.lightColor.g + 0.114 * lightingData.lightColor.b;
            lightingData.lightColor = max(0, lerp(lightingData.lightColor, min(lightingData.lightColor, lightingData.lightColor / pureIntencity * _LightIntensityClamp), _Is_Filter_LightColor));
            half3 addLightColor = FernMainLightDirectLighting(brdfData, brdfDataClearCoat, input, inputData, surfData, lightingData);
            additionLightColor += addLightColor;
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            LightingData lightingData = InitializeLightingData(light, input, inputData.normalWS, inputData.viewDirectionWS, addInputData);
            half radiance = LightingRadiance(lightingData, _UseHalfLambert, surfData.occlusion, _UseRadianceOcclusion);
            // Additional Light Filter Referenced from https://github.com/unity3d-jp/UnityChanToonShaderVer2_Project
            float pureIntencity = 0.299 * lightingData.lightColor.r + 0.587 * lightingData.lightColor.g + 0.114 * lightingData.lightColor.b;
            lightingData.lightColor = max(0, lerp(lightingData.lightColor, min(lightingData.lightColor, lightingData.lightColor / pureIntencity * _LightIntensityClamp), _Is_Filter_LightColor));
            half3 addLightColor = FernMainLightDirectLighting(brdfData, brdfDataClearCoat, input, inputData, surfData, lightingData);
            additionLightColor += addLightColor;
        }
    LIGHT_LOOP_END
    #endif

    // vertex lighting only lambert diffuse for now...
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        additionLightColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

    return additionLightColor;
}

half3 FernRimLighting(LightingData lightingData, InputData inputData, Varyings input, FernAddInputData addInputData)
{
    half3 rimColor = 0;

    #if _FRESNELRIM
        half ndv4 = Pow4(1 - lightingData.NdotVClamp);
        rimColor = LinearStep(_RimThreshold, _RimThreshold + _RimSoftness, ndv4);
        rimColor *= LerpWhiteTo(lightingData.NdotLClamp, _RimDirectionLightContribution);
    #elif _SCREENSPACERIM
        half depthRim = DepthRim(_DepthRimOffset, _DepthOffsetRimReverseX, _DepthRimThresoldOffset, input.positionCS.xy, lightingData.lightDir, addInputData);
        rimColor = depthRim;
    #endif
    rimColor *= _RimColor.rgb;
    return rimColor;
}

half3 FernIndirectLighting(BRDFData brdfData, InputData inputData, Varyings input, half occlusion)
{
    half3 indirectDiffuse = inputData.bakedGI * occlusion;
    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);
    #if _RENDERENVSETTING || _CUSTOMENVCUBE
        half3 indirectSpecular = NPRGlossyEnvironmentReflection(reflectVector, inputData.positionWS, inputData.normalizedScreenSpaceUV, brdfData.perceptualRoughness, occlusion);
    #else
    half3 indirectSpecular = 0;
    #endif
    half3 indirectColor = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    #if _MATCAP
        half3 matCap = SamplerMatCap(_MatCapColor, input.uv.zw, inputData.normalWS, inputData.normalizedScreenSpaceUV, TEXTURE2D_ARGS(_MatCapTex, sampler_MatCapTex));
        indirectColor += lerp(matCap, matCap * brdfData.diffuse, _MatCapAlbedoWeight);
    #endif

    return indirectColor;
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
