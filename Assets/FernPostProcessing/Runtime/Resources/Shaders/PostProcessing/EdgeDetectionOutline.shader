Shader "Hidden/FernNPR/PostProcess/EdgeDetectionOutline"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

    TEXTURE2D_X(_MainTex);
    
    float4 _MainTex_TexelSize;
    float4 _Threshold;
    float3 _Color;

    struct PostProcessVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    struct FullScreenTrianglePostProcessAttributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    float3 SampleSceneNormals(float2 uv, TEXTURE2D_X_FLOAT(_Texture), SAMPLER(sampler_Texture))
    {
        return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D_X(_Texture, sampler_Texture, UnityStereoTransformScreenSpaceTex(uv)).xy) * float3(1.0, 1.0, -1.0);
    }

    float SampleSceneDepth(float2 uv, TEXTURE2D_X_FLOAT(_Texture), SAMPLER(sampler_Texture))
    {
        return SAMPLE_TEXTURE2D_X(_Texture, sampler_Texture, UnityStereoTransformScreenSpaceTex(uv)).r;
    }

    SAMPLER(sampler_linear_clamp);

    // this method from https://github.com/yahiaetman/URPCustomPostProcessingStack"
    float4 SampleSceneDepthNormal(float2 uv){
        float depth = SampleSceneDepth(uv, _CameraDepthTexture, sampler_linear_clamp);
        float depthEye = LinearEyeDepth(depth, _ZBufferParams);
        float3 normal = SampleSceneNormals(uv, _CameraNormalsTexture, sampler_linear_clamp);
        return float4(normal, depthEye);
    }

    float4 SampleNeighborhood(float2 uv, float thickness){
        const float2 offsets[8] = {
            float2(-1, -1),
            float2(-1, 0),
            float2(-1, 1),
            float2(0, -1),
            float2(0, 1),
            float2(1, -1),
            float2(1, 0),
            float2(1, 1)
        };
        
        float2 delta = _MainTex_TexelSize.xy * thickness;
        float4 sum = 0;
        float weight = 0;
        // this method ref from https://github.com/yahiaetman/URPCustomPostProcessingStack"
        for(int i=0; i<8; i++){
            float4 sample = SampleSceneDepthNormal(uv + delta * offsets[i]);
            sum += sample / sample.w; // for perspective
            weight += 1/sample.w;
        }
        sum /= weight;
        return sum;
    }

    float4 Sample4Neighborhood(float2 uv, float thickness){
        const float2 offsets[4] = {
            float2(-1, 0),
            float2(0, -1),
            float2(0, 1),
            float2(1, 0),
        };
        
        float2 delta = _MainTex_TexelSize.xy * thickness;
        float4 sum = 0;
        float weight = 0;
        // this method ref from https://github.com/yahiaetman/URPCustomPostProcessingStack"
        for(int i=0; i<4; i++){
            float4 sample = SampleSceneDepthNormal(uv + delta * offsets[i]);
            sum += sample / sample.w; // for perspective
            weight += 1/sample.w;
        }
        sum /= weight;
        return sum;
    }

    PostProcessVaryings FullScreenTrianglePostProcessVertex (FullScreenTrianglePostProcessAttributes input)
    {
        PostProcessVaryings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    float4 EdgeDetectionFragment (PostProcessVaryings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float4 center = SampleSceneDepthNormal(uv);
        #if _LowQuality
                float4 neighborhood = Sample4Neighborhood(uv,  _Threshold.y);
        #else
            float4 neighborhood = SampleNeighborhood(uv,  _Threshold.y);
        #endif
        float normalSampler = smoothstep(_Threshold.x, 1, dot(center.xyz, neighborhood.xyz));
        float depthSampler = smoothstep(_Threshold.z * center.w, 0.0001f * center.w, abs(center.w - neighborhood.w));
        float edge = 1 - normalSampler * depthSampler;

        float4 color = LOAD_TEXTURE2D_X(_MainTex, uv * _ScreenSize.xy);
        color.rgb = lerp(color.rgb, _Color, edge * _Threshold.w);
        return color;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma
            #pragma shader_feature_local _LowQuality
            #pragma vertex FullScreenTrianglePostProcessVertex
            #pragma fragment EdgeDetectionFragment
            ENDHLSL
        }
    }
    Fallback Off
}
