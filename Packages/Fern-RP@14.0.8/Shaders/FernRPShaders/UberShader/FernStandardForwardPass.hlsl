#ifndef UNIVERSAL_FORWARD_UBERSTANDARD_PASS_INCLUDED
#define UNIVERSAL_FORWARD_UBERSTANDARD_PASS_INCLUDED

#include "../ShaderLibrary/FernCommon.hlsl"
#if _NPR
#include "../ShaderLibrary/NPRVarying.hlsl"
#include "../ShaderLibrary/NPRLighting.hlsl"
#elif _MIRCOGARIN
#include "../ShaderLibrary/MicroGarinVarying.hlsl"
#include "../ShaderLibrary/MicrograinLighting.hlsl" 
#endif

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL) || defined(_KAJIYAHAIR)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

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

    half3 vertexLight = FernVertexLighting(vertexInput.positionWS, normalInput.normalWS);

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

    output.positionWS = vertexInput.positionWS;
    
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

    output.positionCS = CalculateClipPosition(output.positionCS, _ZOffset);
    output.positionCS = PerspectiveRemove(output.positionCS, output.positionWS.xyz, input.positionOS.xyz);

    return output;
}

void LitPassFragment(
    Varyings input, half facing : VFACE
    , out half4 outColor : SV_Target0
    #ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
    #endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData;
    FernAddInputData addInputData;
    PreInitializeInputData(input, facing, inputData, addInputData);

    FernSurfaceData surfaceData;
    InitializeNPRStandardSurfaceData(input.uv.xy, inputData, surfaceData);

    InitializeInputData(input, surfaceData.normalTS, addInputData, inputData);

    #if _SPECULARAA
    surfaceData.smoothness = SpecularAA(inputData.normalWS, surfaceData.smoothness);
    #endif

    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif

    half4 shadowMask = CalculateShadowMask(inputData);

    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    #if defined(_SCREEN_SPACE_OCCLUSION)
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
        mainLight.color *= aoFactor.directAmbientOcclusion;
        surfaceData.occlusion = min(surfaceData.occlusion, aoFactor.indirectAmbientOcclusion);
    #else
    AmbientOcclusionFactor aoFactor;
    aoFactor.indirectAmbientOcclusion = 1;
    aoFactor.directAmbientOcclusion = 1;
    #endif

    BRDFData brdfData, clearCoatbrdfData;
    FernInitializeBRDFData(surfaceData, brdfData, clearCoatbrdfData);

    LightingData lightingData = InitializeLightingData(mainLight, input, inputData.normalWS, inputData.viewDirectionWS,
                                                       addInputData);

    half4 color = 1;
    color.rgb = FernMainLightDirectLighting(brdfData, clearCoatbrdfData, input, inputData, surfaceData, lightingData);
    color.rgb += FernAdditionLightDirectLighting(brdfData, clearCoatbrdfData, input, inputData, surfaceData,
                                             addInputData, shadowMask, meshRenderingLayers, aoFactor);
    color.rgb += FernIndirectLighting(brdfData, inputData, input, surfaceData.occlusion);
    color.rgb += FernRimLighting(lightingData, inputData, input, addInputData); 

    color.rgb += surfaceData.emission;
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    color.a = surfaceData.alpha;

    outColor = color;

    #ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}

void LitPassFragment_DepthPrePass(
    Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    outColor = 0;

    InputData inputData;
    FernAddInputData addInputData;
    PreInitializeInputData(input, 1, inputData, addInputData);

    FernSurfaceData surfaceData;
    InitializeNPRStandardSurfaceData(input.uv.xy, inputData, surfaceData);

    clip(surfaceData.alpha - _Cutoff);
}

#endif // UNIVERSAL_FORWARD_UBERSTANDARD_PASS_INCLUDED
