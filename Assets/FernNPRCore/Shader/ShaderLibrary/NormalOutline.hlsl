#ifndef UNIVERSAL_NORMALOUTLINE_INCLUDED
#define UNIVERSAL_NORMALOUTLINE_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    #if defined(UV2_AS_NORMALS)
        float4 uv2 : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float GetOutlineVertex_ScreenCoordinatesWidth(const float4 positionCS)
{
    const float maxViewFrustumPlaneHeight = 2.0f;
    const float invTangentHalfVerticalFov = unity_CameraProjection[1][1];
    const float widthScaledMaxDistance = maxViewFrustumPlaneHeight * invTangentHalfVerticalFov * 0.5f;
    return min(positionCS.w, widthScaledMaxDistance);
}

Varyings NormalOutLineVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if !_OUTLINE
        return output;
    #else
        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
        float4 positionCS = vertexInput.positionCS;
        output.positionWS = vertexInput.positionWS;
        float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));
        half aspect = abs(nearUpperRight.y / nearUpperRight.x);

        half3 normalVS = normalize(mul((half3x3)UNITY_MATRIX_IT_MV, input.normalOS));
        half3 normalCS = mul((float3x3)UNITY_MATRIX_P, normalVS);

        half2 normalProjectedCS = normalize(normalCS.xy);
        float clipSpaceHeight = 0.02f;
        normalProjectedCS *= clipSpaceHeight * _OutlineWidth * GetOutlineVertex_ScreenCoordinatesWidth(positionCS);
        normalProjectedCS.x *= aspect;
        normalProjectedCS.xy *= saturate(1 - normalVS.z * normalVS.z);
        output.positionCS = float4(positionCS.xy + normalProjectedCS.xy, positionCS.zw);

        output.positionCS = PerspectiveRemove(output.positionCS, output.positionWS, input.positionOS);


        return output;
    #endif
}

half4 NormalOutlineFragment(Varyings input) : SV_Target
{
    #if defined(_OUTLINE)
        half4 outlineColor = 0;
        outlineColor.rgb = _OutlineColor.rgb;
        outlineColor.a = _BaseColor.a;
        return outlineColor;
    #else
        return 0;
    #endif
}

#endif
