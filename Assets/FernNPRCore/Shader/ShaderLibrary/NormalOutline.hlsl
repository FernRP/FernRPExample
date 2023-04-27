#ifndef UNIVERSAL_NORMALOUTLINE_INCLUDED
#define UNIVERSAL_NORMALOUTLINE_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 smoothedNormal:TEXCOORD7;
    float4 color : COLOR;

    #if defined(UV2_AS_NORMALS)
        float4 uv2 : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalDir : TEXCOORD1;
    float3 tangentDir : TEXCOORD2;
    float3 bitangentDir : TEXCOORD3;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 OctahedronToUnitVector(float2 Oct)
{
    float3 N = float3(Oct, 1 - dot(1, abs(Oct)));
    if (N.z < 0)
    {
        N.xy = (1 - abs(N.yx)) * (N.xy >= 0 ? float2(1, 1) : float2(-1, -1));
    }
    return normalize(N);
}

float3 TransformTBN(float2 bakedNormal, float3x3 tbn)
{
    float3 normal = OctahedronToUnitVector(bakedNormal);
    return  (mul(normal, tbn));
}
Varyings NormalOutLineVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if _OUTLINE
        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
        float4 positionCS = vertexInput.positionCS;
        float3 normalOS = normalize(input.normalOS);
        #if _SMOOTHEDNORMAL
            float3 tangentOS = input.tangentOS;
            tangentOS = normalize(tangentOS);
            float3 bitangentOS = normalize(cross(normalOS, tangentOS) * input.tangentOS.w);
            float3x3 tbn = float3x3(tangentOS, bitangentOS, normalOS);
            float3 smoothedNormal = (TransformTBN(input.smoothedNormal, tbn));
            normalOS = smoothedNormal;
        #endif
            float Set_OutlineWidth = positionCS.w * _OutlineWidth;
            Set_OutlineWidth = min(Set_OutlineWidth, _OutlineWidth);
            Set_OutlineWidth *= _OutlineWidth;
            Set_OutlineWidth = min(Set_OutlineWidth, _OutlineWidth) * 0.001;
        #if _OUTLINEWIDTHWITHVERTEXTCOLORA
            Set_OutlineWidth *= input.color.a;
        #elif _OUTLINEWIDTHWITHUV8A
            Set_OutlineWidth *= input.smoothedNormal.a;
        #endif
        output.positionCS = TransformObjectToHClip(input.positionOS + normalOS * Set_OutlineWidth);
        output.positionCS = PerspectiveRemove(output.positionCS, vertexInput.positionWS, input.positionOS);

        output.color = input.color;
        output.uv = input.texcoord;
    #endif
    return output;
}

half4 NormalOutlineFragment(Varyings input) : SV_Target
{
    #if defined(_OUTLINE)
        half4 outlineColor = 0;
        outlineColor.rgb = _OutlineColor.rgb;
        half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
        outlineColor.a = albedoAlpha.a;
        #if _OUTLINECOLORBLENDBASEMAP
            outlineColor.rgb *= albedoAlpha.rgb * albedoAlpha.rgb;
        #elif _OUTLINECOLORBLENDVERTEXCOLOR
            outlineColor.rgb *= input.color.rgb;
            outlineColor.a = input.color.a;
        #endif
        clip(outlineColor.a - _Cutoff);
        return outlineColor;
    #else
        return 0;
    #endif
}

#endif
