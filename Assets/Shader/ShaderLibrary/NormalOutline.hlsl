#ifndef UNIVERSAL_NORMALOUTLINE_INCLUDED
#define UNIVERSAL_NORMALOUTLINE_INCLUDED

half3 _OutlineColor;
half _OutlineWidth;

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
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

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
        output.positionCS = vertexInput.positionCS;
        float2 clipNormals = normalize(mul(UNITY_MATRIX_MVP, float4(input.normalOS,0)).xy);
        float screenRatio = _ScreenParams.x / _ScreenParams.y;
        output.positionCS.xy += clipNormals.xy * (_OutlineWidth * 0.01) * float2(1.0, screenRatio);
        return output;
    #endif
}

half4 NormalOutlineFragment(Varyings input) : SV_Target
{
    #if defined(_OUTLINE)
        half4 outlineColor = 0;
        outlineColor.rgb = _OutlineColor.rgb;
        outlineColor.a = 1;
        return outlineColor;
    #else
        return 0;
    #endif
}

#endif
