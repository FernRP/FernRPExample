#ifndef UNIVERSAL_FORWARD_NPRSTANDARD_PASS_INCLUDED
#define UNIVERSAL_FORWARD_NPRSTANDARD_PASS_INCLUDED

#include "../ShaderLibrary/NPRLighting.hlsl"

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL) || defined(_KAJIYAHAIR)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uv                       : TEXCOORD0; // zw：MatCap

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
#endif

    float3 normalWS                 : TEXCOORD2;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
#endif
    float3 viewDirWS                : TEXCOORD4;

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void PreInitializeInputData(Varyings input, out InputData inputData, out NPRAddInputData addInputData)
{
    inputData = (InputData)0;
    addInputData = (NPRAddInputData)0;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
    #endif

    #if defined(_NORMALMAP) || defined(_DETAIL)
        float sgn = input.tangentWS.w;      // should be either +1 or -1
        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        inputData.tangentToWorld = tangentToWorld;
    #endif
    
    inputData.normalWS = input.normalWS;

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

    addInputData.linearEyeDepth = DepthSamplerToLinearDepth(input.positionCS.z);
}

void InitializeInputData(Varyings input, half3 normalTS, inout NPRAddInputData addInputData, inout InputData inputData)
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

///////////////////////////////////////////////////////////////////////////////
//                         Shading Function                                  //
///////////////////////////////////////////////////////////////////////////////

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

half3 NPRSpecularLighting(BRDFData brdfData, NPRSurfaceData surfData, Varyings input, InputData inputData, half3 albedo, half radiance, LightingData lightData)
{
    half3 specular = 0;
    #if _GGX
        specular = GGXDirectBRDFSpecular(brdfData, lightData.LdotHClamp, lightData.NdotHClamp) * surfData.specularIntensity;
    #elif _STYLIZED
        specular = StylizedSpecular(albedo, lightData.NdotHClamp, _StylizedSpecularSize, _StylizedSpecularSoftness, _StylizedSpecularAlbedoWeight) * surfData.specularIntensity;
    #elif _BLINNPHONG
        specular = BlinnPhongSpecular((1 - brdfData.perceptualRoughness) * _Shininess, lightData.NdotHClamp) * surfData.specularIntensity;
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

half3 NPRDirectLighting(BRDFData brdfData, BRDFData brdfDataClearCoat, Varyings input, InputData inputData, NPRSurfaceData surfData, half radiance, LightingData lightData)
{
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

half3 NPRRimLighting(LightingData lightingData, InputData inputData, Varyings input, NPRAddInputData addInputData)
{
    half3 rimColor = 0;

    #if _FRESNELRIM
        half ndv4 = Pow4(1 - lightingData.NdotVClamp);
        rimColor = LinearStep(_RimThreshold, _RimThreshold + _RimSoftness, ndv4);
        rimColor *= LerpWhiteTo(lightingData.NdotLClamp, _RimDirectionLightContribution);
    #elif _SCREENSPACERIM
        half depthRim = DepthRim(_DepthRimOffset, _DepthRimThresoldOffset, _DepthRimLightCameraDistanceStart,
            _DepthRimLightCameraDistanceFadeoutEnd, input.positionCS.xy, lightingData.lightDir, addInputData);
        rimColor = depthRim;
    #endif
    rimColor *=  _RimColor.rgb;
    return rimColor;
}

half3 NPRIndirectLighting(BRDFData brdfData, InputData inputData, Varyings input, half occlusion)
{
    half3 indirectDiffuse = inputData.bakedGI * occlusion;
    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);
    #if _RENDERENVSETTING || _CUSTOMENVCUBE
        half3 indirectSpecular = NPRGlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion);
    #else
        half3 indirectSpecular = 0;
    #endif
    half3 indirectColor = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    #if _MATCAP
        half3 matCap = SamplerMatCap(_MatCapColor, input.uv.zw, inputData.normalWS, inputData.normalizedScreenSpaceUV, TEXTURE2D_ARGS(_MatCapTex, sampler_MatCapTex));
        indirectColor += matCap;
    #endif
    
    return indirectColor;
}

LightingData InitializeLightingData(Light mainLight, Varyings input, half3 normalWS, half3 viewDirectionWS, NPRAddInputData addInputData)
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
        lightData.ShadowAttenuation = DepthShadow(_DepthShadowOffset, _DepthShadowThresoldOffset, _DepthShadowSoftness, input.positionCS.xy, mainLight.direction, addInputData);
    #else
        lightData.ShadowAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    #endif

    return lightData;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    #if _MATCAP
        half3 normalVS = mul((float3x3)UNITY_MATRIX_V, output.normalWS.xyz);
        float4 screenPos = ComputeScreenPos(output.positionCS);
        float3 perspectiveOffset = (screenPos.xyz / screenPos.w) - 0.5;
        normalVS.xy -= (perspectiveOffset.xy * perspectiveOffset.z) * 0.5;
        output.uv.zw = normalVS.xy * 0.5 + 0.5;
        output.uv.zw = output.uv.zw.xy * _MatCapTex_ST.xy + _MatCapTex_ST.zw;
    #endif
    
    #if _SDFFACE
        SDFFaceUV(_SDFDirectionReversal, _SDFFaceArea, output.uv.zw);
    #endif
    
    return output;
}

half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData;
    NPRAddInputData addInputData;
    PreInitializeInputData(input, inputData, addInputData);

    NPRSurfaceData surfaceData;
    InitializeNPRStandardSurfaceData(input.uv.xy, inputData, surfaceData);
   
    InitializeInputData(input, surfaceData.normalTS, addInputData, inputData);

    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    NPRMainLightCorrect(_LightDirectionObliqueWeight, mainLight);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    BRDFData brdfData, clearCoatbrdfData;
    InitializeNPRBRDFData(surfaceData, brdfData, clearCoatbrdfData);

    LightingData lightingData = InitializeLightingData(mainLight, input, inputData.normalWS, inputData.viewDirectionWS, addInputData);

    half radiance = LightingRadiance(lightingData, _UseHalfLambert, surfaceData.occlusion, _UseRadianceOcclusion);
    half4 color = 1;
    color.rgb = NPRDirectLighting(brdfData, clearCoatbrdfData, input, inputData, surfaceData, radiance, lightingData);
    color.rgb += NPRIndirectLighting(brdfData, inputData, input, surfaceData.occlusion);
    color.rgb += NPRRimLighting(lightingData, inputData, input, addInputData);
    color.rgb += surfaceData.emission;
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    color.a = surfaceData.alpha;

    return color;
}

#endif
