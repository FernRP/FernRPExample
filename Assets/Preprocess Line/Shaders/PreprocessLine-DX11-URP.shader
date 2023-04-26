Shader "Hidden/PreprocessLine-DX11" {
    Properties {
    }

    HLSLINCLUDE
    StructuredBuffer<float3> vertices;
    StructuredBuffer<float3> normals;
    StructuredBuffer<float4> colors;
    int4 debugLineType = 0;  // isBoundary, isOutline, isCrease
    float4 lineColor = 0;
    float lineWidth = 0.001;
    ENDHLSL

    SubShader{
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass {
            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma target 2.5
            #pragma vertex vertexShader
            #pragma fragment fragmentShader
            #pragma geometry geometryShader

            struct g2f {
                float4 vertex : SV_POSITION;
            };

            struct v2g {
                float4 vertex1 : POSITION;
                float4 vertex2 : COLOR0;
                int isLine : COLOR1;
            };

            struct Line {
                int vertex1;
                int vertex2;
                int triangle1Vertex;
                int triangle2Vertex;
            };

            StructuredBuffer<Line> degradedQuads;

            v2g vertexShader(uint id : SV_VertexID, uint inst : SV_InstanceID) {
                v2g o;
                Line degradedQuad = degradedQuads[id];
                float4 vertex1         = float4(vertices[degradedQuad.vertex1], 1.0f);
                float4 vertex2         = float4(vertices[degradedQuad.vertex2], 1.0f);
                float4 triangle1Vertex = float4(vertices[degradedQuad.triangle1Vertex], 1.0f);
                float3 vertex1Normal   = normals[degradedQuad.vertex1];
                float3 vertex2Normal   = normals[degradedQuad.vertex2];
//                float3 viewTriangle1   = ObjSpaceViewDir((vertex1 + vertex2 + triangle1Vertex) / 3.0f);
                float4 centerPoint     = (vertex1 + vertex2 + triangle1Vertex) / 3.0f;
                float3 viewTriangle1   = TransformWorldToObject(GetCameraPositionWS()).xyz - centerPoint.xyz;
                float4 vertex1Color    = colors[degradedQuad.vertex1];
                float4 vertex2Color    = colors[degradedQuad.vertex2];

                bool isBoundary = !step(0, degradedQuad.triangle2Vertex); // degradedQuad.triangle2Vertex < 0

                float4 triangle2Vertex = float4(vertices[degradedQuad.triangle2Vertex * !isBoundary], 1.0f);

                float3 v1 = (vertex2 - vertex1).xyz;
                float3 v2 = (triangle1Vertex - vertex1).xyz;
                float3 v3 = (triangle2Vertex - vertex1).xyz;

                float3 face1Normal = cross(v1, v2);
                float3 face2Normal = cross(v3, v1);

                // !step(xx, 0) => xx > 0, step(xx, 0) => xx <= 0
                bool isOutline = !isBoundary * !step(0, dot(face1Normal, viewTriangle1) * dot(face2Normal, viewTriangle1));
                // pow(dot(face1Normal, face2Normal) / cos(degree), 2)在[0, PI/2]上单调递增, 可避免开方
                bool isCrease  = !isBoundary * step(pow(dot(face1Normal, face2Normal) / cos(1.0472f), 2), dot(face1Normal, face1Normal) * dot(face2Normal, face2Normal));

                //如果顶点1和顶点2被人工绘制了颜色，则该边是错误的边。
                isBoundary = isBoundary * step(vertex1Color.r * vertex2Color.r, 0) * debugLineType.x;
                isOutline  = isOutline * step(vertex1Color.g * vertex2Color.g, 0) * debugLineType.y;
                isCrease   = isCrease * step(vertex1Color.b * vertex2Color.b, 0) * debugLineType.z;
                bool isForced   = !step(vertex1Color.a * vertex2Color.a, 0) * debugLineType.w;

                bool isLine = isBoundary | isOutline | isCrease;

                //把线往模型法线方向移出一点，防止线画和面共面而穿模
                o.vertex1 = TransformWorldToHClip(TransformObjectToWorld(vertex1.xyz + vertex1Normal * 0.001f));
                o.vertex2 = TransformWorldToHClip(TransformObjectToWorld(vertex2.xyz + vertex2Normal * 0.001f));
                o.isLine = isLine;

                return o;
            }

            /*
            [maxvertexcount(2)]
            void geometryShader(point v2g input[1], inout LineStream<g2f> stream) {
                if (input[0].isLine == 0) {
                    return;
                }
                g2f o;
                o.vertex = input[0].vertex1;
                stream.Append(o);
                o.vertex = input[0].vertex2;
                stream.Append(o);
                stream.RestartStrip();
            }
            */
            
            [maxvertexcount(6)]
            void geometryShader(point v2g input[1], inout TriangleStream<g2f> stream) {
                if (input[0].isLine == 0) {
                    return;
                }
                g2f o;

                float3 e0 = input[0].vertex1.xyz / input[0].vertex1.w;
                float3 e1 = input[0].vertex2.xyz / input[0].vertex2.w;

                float2 v = normalize(e1.xy - e0.xy);
                float2 n = float2(-v.y, v.x) * lineWidth * (e0.z + e1.z + 1) * 0.5f;

                float2 ext = 0.001 * v;
                float4 v0 = float4(e0.xy + n * 0.5 - ext, e0.z, 1.0) * input[0].vertex1.w;
                float4 v1 = float4(e0.xy - n * 0.5 - ext, e0.z, 1.0) * input[0].vertex1.w;
                float4 v2 = float4(e1.xy + n * 0.5 + ext, e1.z, 1.0) * input[0].vertex2.w;
                float4 v3 = float4(e1.xy - n * 0.5 + ext, e1.z, 1.0) * input[0].vertex2.w;
                
                o.vertex = v0;
                stream.Append(o);
                o.vertex = v3;
                stream.Append(o);
                o.vertex = v2;
                stream.Append(o);
                stream.RestartStrip();

                o.vertex = v0;
                stream.Append(o);
                o.vertex = v1;
                stream.Append(o);
                o.vertex = v3;
                stream.Append(o);
                stream.RestartStrip();
            }

            float4 fragmentShader(g2f i) : SV_Target {
                return lineColor;
            }

            ENDHLSL
        }
    }
}
