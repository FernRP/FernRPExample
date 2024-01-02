#ifndef UNIVERSAL_FERNLITTING_INPUT_INCLUDED
#define UNIVERSAL_FERNLITTING_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "../ShaderLibrary/DeclareDepthShadowTexture.hlsl"
#include "FernShaderUtils.hlsl"
#include "../ShaderLibrary/MicroGarinBSDF.hlsl"

//paper: https://hal.science/hal-04220006

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////

// https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
float3 FresnelDielectricConductor(float3 Eta, float3 Etak, float CosTheta)
{  
    float CosTheta2 = CosTheta * CosTheta;
    float SinTheta2 = 1. - CosTheta2;
    float3 Eta2 = Eta * Eta;
    float3 Etak2 = Etak * Etak;

    float3 t0 = Eta2 - Etak2 - SinTheta2;
    float3 a2plusb2 = sqrt(t0 * t0 + 4. * Eta2 * Etak2);
    float3 t1 = a2plusb2 + CosTheta2;
    float3 a = sqrt(0.5f * (a2plusb2 + t0));
    float3 t2 = 2. * a * CosTheta;
    float3 Rs = (t1 - t2) / (t1 + t2);

    float3 t3 = CosTheta2 * a2plusb2 + SinTheta2 * SinTheta2;
    float3 t4 = t2 * SinTheta2;   
    float3 Rp = Rs * (t3 - t4) / (t3 + t4);

    return 0.5 * (Rp + Rs);
}

// https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
float3 FresnelDielectricDielectric(float3 Eta, float CosTheta)
{
    float SinTheta2 = 1. - CosTheta * CosTheta;

    float3 t0 = sqrt(1. - (SinTheta2 / (Eta * Eta)));
    float3 t1 = Eta * t0;
    float3 t2 = Eta * CosTheta;

    float3 rs = (CosTheta - t1) / (CosTheta + t1);
    float3 rp = (t0 - t2) / (t0 + t2);

    return 0.5 * (rs * rs + rp * rp);
}


/**
 * \brief Eq. 16
 * \param tau_0 Micrograin distribution density
 * \param beta_sqr roughness_porous4
 * \param cos_theta_m 
 * \return 
 */
float D_our(float tau_0, float beta_sqr, float cos_theta_m)
{
    float cos2_theta_m = cos_theta_m * cos_theta_m;
    float tan2_theta_m = (1. - cos2_theta_m) / cos2_theta_m;
    float tmp = beta_sqr + tan2_theta_m;
    float num = beta_sqr * log(1. - tau_0) * pow(1. - tau_0,tan2_theta_m / tmp);
    float denum = tau_0 * PI * tmp * tmp * cos2_theta_m * cos2_theta_m;
    return -num / denum;
}

// Eq. in supplemental
float G1_our(float tau_0, float beta_sqr, float cos_theta)
{ 
    float pi_gamma = -log(1. - tau_0);
    float exp_pi_gamma_minus_one = exp(pi_gamma) - 1.;

    cos_theta = clamp(cos_theta,0.00001,0.99999);
    float mu  = cos_theta / sqrt(1. - cos_theta * cos_theta);
    
    float beta2  = beta_sqr;
    float beta4  = beta2  * beta2;
    float beta6  = beta4  * beta2;
    float beta8  = beta6  * beta2;
    float beta10 = beta8  * beta2;
    float beta12 = beta10 * beta2;
    
    float mu2  = mu   * mu;
    float mu4  = mu2  * mu2;
    float mu6  = mu4  * mu2;
    float mu8  = mu6  * mu2;
    float mu10 = mu8  * mu2;
    float mu12 = mu10 * mu2;
    
    float beta2_mu2 = beta2 + mu2;
    float sqrt_beta2_mu2 = sqrt(beta2_mu2);
    
    float F0 = pi_gamma * (-mu + sqrt_beta2_mu2)/(2.*mu);

    float F1 = pow(pi_gamma,2.) * (beta2+2. * mu * (mu-sqrt_beta2_mu2))/(8. * mu * sqrt_beta2_mu2);

    float F2 = pow(pi_gamma,3.) * (3. * beta4+12. * beta2 * mu2+8. * mu4-8. * mu * pow(beta2_mu2,3./2.))/(96. * mu * pow(beta2_mu2,3./2.));

    float F3 = pow(pi_gamma,4.) * (5. * beta6+30. * beta4 * mu2+40. * beta2 * mu4+16. * mu6-16. * mu * pow(beta2_mu2,5./2.))/(768. * mu * pow(beta2_mu2,5./2.));

    float F4 = pow(pi_gamma,5.) * (35. * beta8+280. * beta6 * mu2+560. * beta4 * mu4+448. * beta2 * mu6+128. * mu8-128. * mu * pow(beta2_mu2,7./2.))/(30720. * mu * pow(beta2_mu2,7./2.));

    float F5 = pow(pi_gamma,6.) * (63. * beta10+630. * beta8 * mu2+1680. * beta6 * mu4+2016. * beta4 * mu6+1152. * beta2 * mu8+256. * mu10-256. * mu * pow(beta2_mu2,9./2.))/(368640. * mu * pow(beta2_mu2,9./2.));

    float F6 = pow(pi_gamma,7.) * (231. * beta12+2772. * beta10 * mu2+9240. * beta8 * mu4+14784. * beta6 * mu6+12672. * beta4 * mu8+5632. * beta2 * mu10 + 1024. * mu12-1024. * mu * pow(beta2_mu2,11./2.))/(10321920. * mu * pow(beta2_mu2,11./2.));
    
    float lambda_ = (F0 + F1 + F2 + F3 + F4 + F5 + F6) / exp_pi_gamma_minus_one;
    
    return 1. / (1. + lambda_);
}

float D_ggx(float alpha_sqr, float cos_theta)
{
    float cos2_theta = cos_theta*cos_theta;
    float tan2_theta = (1.-cos2_theta) / cos2_theta;
   
    float denom = PI * cos2_theta * cos2_theta * (alpha_sqr + tan2_theta)*(alpha_sqr + tan2_theta);
    
    return alpha_sqr / denom;
}


float lambda_ggx(float alpha_sqr, float tan_theta)
{
    return (-1. + sqrt(1.+alpha_sqr*tan_theta*tan_theta))/2.;
}

float G1_ggx(float alpha_sqr, float cos_theta)
{
    float tan_theta = sqrt(1. - cos_theta * cos_theta) / cos_theta;
    return 1./(1.+lambda_ggx(alpha_sqr,tan_theta));
}

float G_ggx(float alpha_sqr, float cos_theta_i, float cos_theta_o)
{
    return G1_ggx(alpha_sqr,cos_theta_i) * G1_ggx(alpha_sqr,cos_theta_o);
}


float G_our(float tau_0, float beta_sqr, float alpha_sqr, float cos_theta_i, float cos_theta_o)
{
    return G1_our(tau_0, beta_sqr, cos_theta_i) * G1_our(tau_0, beta_sqr, cos_theta_o);
}


// Eq 18.
float alpha2beta(float alpha,float tau_0)
{
    float fac = sqrt(-tau_0 / log(1. - tau_0));
    return alpha / fac;
}

// Eq 18.
float beta2alpha(float beta, float tau_0)
{
    float fac = sqrt(-tau_0 / log(1. - tau_0));
    return beta * fac;
}
    
// Eq. in section visible filling factor
float gamma_beta(float beta_sqr,float cos_theta)
{
    float cos2_theta = cos_theta * cos_theta;
    float sin2_theta = 1. - cos2_theta;
    return sqrt(beta_sqr * sin2_theta + cos2_theta);
}

// Eq. in section Visible filling factor
float gamma_beta_plus(float beta_sqr, float cos_theta)
{
    return 0.5 * (cos_theta + gamma_beta(beta_sqr,cos_theta));
}

// Eq. 21
float tau_theta(float tau_0, float beta_sqr, float cos_theta)
{
    return 1. - pow((1. - tau_0) , (gamma_beta(beta_sqr,cos_theta)/cos_theta));
}


// Eq. 22
float tau_theta_plus(float tau_0, float beta_sqr, float cos_theta)
{
    return 1. - sqrt((1. - tau_theta(tau_0, beta_sqr, cos_theta))*(1. - tau_0));
}

// Eq. 24
float visibility_weight(float tau_0, float beta_sqr, float cos_theta_i, float cos_theta_o)
{
    float cos_theta_i_ = clamp(abs(cos_theta_i),0.00001,1.);
    float cos_theta_o_ = clamp(abs(cos_theta_o),0.00001,1.);
    return 1. - ((1. - tau_theta_plus(tau_0,beta_sqr, cos_theta_i_)) * (1. - tau_theta_plus(tau_0,beta_sqr, cos_theta_o_)) / (1.- tau_0));
}

// Eq. 1
float3 visibility_blend_our(float tau_0, float beta_sqr, float cos_theta_i, float cos_theta_o, float3 brdf_s, float3 brdf_b)
{
    return lerp(brdf_b, brdf_s, visibility_weight(tau_0, beta_sqr, cos_theta_i, cos_theta_o));
}

float3 micrograin_conductor_bsdf(
      float tau_0
    , float beta_sqr
    , float alpha_sqr
    , float3 R0
    , float cos_theta_i
    , float cos_theta_o
    , float cos_theta_h
    , float cos_theta_d)
{    
    float D = D_our(tau_0,beta_sqr,cos_theta_h);
    float G = G_our(tau_0, beta_sqr, alpha_sqr, cos_theta_i,cos_theta_o);
    float3 eta = (1. + sqrt(R0))/(1. - sqrt(R0));
    float3 F = FresnelDielectricDielectric(eta,cos_theta_d);
    return D*G*F / (4. * cos_theta_i * cos_theta_o);
}

/**
 * \brief 
 * \param tau_0 
 * \param beta_sqr roughness_porous4
 * \param alpha_sqr roughness_base4
 * \param R0 
 * \param kd 
 * \param cos_theta_i 
 * \param cos_theta_o 
 * \param cos_theta_h 
 * \param cos_theta_d 
 * \return 
 */
float3 micrograin_plastic_bsdf(
      float tau_0
    , float beta_sqr
    , float alpha_sqr
    , float3 R0
    , float3 kd
    , float cos_theta_i
    , float cos_theta_o
    , float cos_theta_h
    , float cos_theta_d)
{
    float3 eta = (1. + sqrt(R0))/(1. - sqrt(R0));
    float D = D_our(tau_0,beta_sqr,cos_theta_h);
    float G = G_our(tau_0, beta_sqr, alpha_sqr, cos_theta_i,cos_theta_o);
    float3 F = FresnelDielectricDielectric(eta,cos_theta_d);
    float3 spec = D*G*F / (4. * cos_theta_i * cos_theta_o);
    
    float3 Ti = 1. - FresnelDielectricDielectric(eta,cos_theta_i);
    float3 To = 1. - FresnelDielectricDielectric(eta,cos_theta_o);
    float3 diff = To * Ti * kd / PI;
    
    return spec + diff;
}

float3 ggx_conductor_brdf(
      float alpha_sqr
    , float3 R0
    , float cos_theta_i
    , float cos_theta_o
    , float cos_theta_h
    , float cos_theta_d)
{    
    float3 eta = (1. + sqrt(R0))/(1. - sqrt(R0));
    float D = D_ggx(alpha_sqr,cos_theta_h);
    float G = G_ggx(alpha_sqr, cos_theta_i,cos_theta_o);
    float3 F = FresnelDielectricDielectric(eta,cos_theta_d);
    return D*G*F / (4. * cos_theta_i * cos_theta_o);
}

float3 ggx_plastic_brdf(
      float alpha_sqr
    , float3 R0
    , float3 kd
    , float cos_theta_i
    , float cos_theta_o
    , float cos_theta_h
    , float cos_theta_d)
{
    float3 eta = (1. + sqrt(R0))/(1. - sqrt(R0));
    float D = D_ggx(alpha_sqr, cos_theta_h);
    float G = G_ggx(alpha_sqr, cos_theta_i,cos_theta_o);
    float3 F = FresnelDielectricDielectric(eta,cos_theta_d);
    float3 spec = D*G*F / (4. * cos_theta_i * cos_theta_o);
    
    float3 Ti = 1. - FresnelDielectricDielectric(eta,cos_theta_i);
    float3 To = 1. - FresnelDielectricDielectric(eta,cos_theta_o);
    float3 diff = To * Ti * kd / PI;
    
    return spec + diff;
}

float3 MicrograinBSDF_Eval(float3 normal, float3 viewDir, float3 lightDir, float tau_0, float roughness_porous2, float roughness_base2
    , float3 diffuse_porous
    , float3 diffuse_base
    , float3 specular_porous
    , float3 specular_base)
{
    float3 wh = normalize(viewDir+lightDir);
    float cos_theta_o = clamp(dot(normal,lightDir) ,0.0,1.0);
    float cos_theta_i = clamp(dot(normal,viewDir) ,0.0,1.0);
    float cos_theta_h = clamp(dot(normal,wh) ,0.0,1.0);
    float cos_theta_d = clamp(dot(wh,viewDir),0.0,1.0);
    roughness_porous2 = clamp(roughness_porous2,0.001,1.);
    tau_0 = clamp(tau_0,0.001,0.999);
    float roughness_porous4 = roughness_porous2*roughness_porous2;
    float roughness_base4 = roughness_base2*roughness_base2;
    
    float3 brdf_s = 
        micrograin_plastic_bsdf(
          tau_0
        , roughness_porous4
        , roughness_base4
        , specular_porous
        , diffuse_porous
        , cos_theta_i
        , cos_theta_o
        , cos_theta_h
        , cos_theta_d);
    
    float3 brdf_b = 
        ggx_plastic_brdf(
        roughness_base4
        , specular_base
        , diffuse_base
        , cos_theta_i
        , cos_theta_o
        , cos_theta_h
        , cos_theta_d);
    
    float3 col = visibility_blend_our(
          tau_0
        , roughness_base4
        , cos_theta_i
        , cos_theta_o
        , brdf_s
        , brdf_b);

    return col * cos_theta_o;
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
    specular *= radiance * brdfData.specular;
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
    float perceptualRoughness_porous = 1 - surfData.porousSmoothness;
    float roughness_porous = max(PerceptualRoughnessToRoughness(perceptualRoughness_porous), HALF_MIN_SQRT);
    float roughness_porous2 = roughness_porous * roughness_porous;
    float perceptualRoughness_base = 1 - surfData.smoothness;
    float roughness_base = max(PerceptualRoughnessToRoughness(perceptualRoughness_base), HALF_MIN_SQRT);
    float roughness_base2 = roughness_base * roughness_base;

    // base brdf
    half oneMinusReflectivity = OneMinusReflectivityMetallic(surfData.metallic);
    half3 brdfDiffuse_base = surfData.albedo * oneMinusReflectivity;
    half3 brdfSpecular_base = lerp(kDieletricSpec.rgb, surfData.albedo, surfData.metallic);

    // porous brdf
    half oneMinusReflectivity_s = OneMinusReflectivityMetallic(surfData.porousMetallic);
    half3 brdfDiffuse_porous = surfData.porousColor * oneMinusReflectivity_s;
    half3 brdfSpecular_porous = lerp(kDieletricSpec.rgb, surfData.porousColor, surfData.porousMetallic);

    half3 brdf = MicrograinBSDF_Eval(inputData.normalWS, inputData.viewDirectionWS, lightData.lightDir,
        surfData.porousDensity, roughness_porous2, roughness_base2,
        brdfDiffuse_porous, brdfDiffuse_base, brdfSpecular_porous, brdfSpecular_base);
    
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
            half radiance = LightingRadiance(lightingData);
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
            half radiance = LightingRadiance(lightingData);
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
            half radiance = LightingRadiance(lightingData);
            // Additional Light Filter Referenced from https://github.com/unity3d-jp/UnityChanToonShaderVer2_Project
            float pureIntencity = 0.299 * lightingData.lightColor.r + 0.587 * lightingData.lightColor.g + 0.114 * lightingData.lightColor.b;
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

#endif // UNIVERSAL_FERNLITTING_INPUT_INCLUDED
