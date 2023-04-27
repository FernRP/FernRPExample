// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Project"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Slider("Slider", Range( 0 , 1)) = 0
		_FocusPoint("FocusPoint", Vector) = (0,0,0,0)
		_PerspectiveRemoveRange("PerspectiveRemoveRange", Float) = 0
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		
		Pass
		{
			Name "Unlit"
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#define ASE_NEEDS_FRAG_POSITION


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				// I don't care instance!!!!!
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				// I don't care instance!!!!!
				//UNITY_VERTEX_INPUT_INSTANCE_ID
				//UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _TextureSample0;
			uniform float4 _TextureSample0_ST;

			//---------------------------------DONE!!!!!------------------------------------------
			uniform float4x4 PMRemove;
			uniform float _Slider;
			uniform float3 _FocusPoint;
			uniform float _PerspectiveRemoveRange;
			//---------------------------------DONE!!!!!------------------------------------------
			
			v2f vert ( appdata v )
			{
				v2f o;
				// I don't care instance!!!!!
				//UNITY_SETUP_INSTANCE_ID(v);
				//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				//UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_texcoord2 = v.vertex;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				float3 vertexValue = float3(0, 0, 0);

				//---------------------------------DONE!!!!!------------------------------------------
				//Transofrm vertex to clip Space
				o.vertex = UnityObjectToClipPos(v.vertex);
				//Transofrm vertex to worldspace(object2world) than ClipSpace(Orth)
				float4 sdasdasd = mul(PMRemove,mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));
				//We got the sdasdasd at OrthClipSpace.
				//We have make sure that the NDC space everything can be linked.
				//So the W might be hard to deal
				float4 clipPositionLerp = lerp( o.vertex ,float4(sdasdasd.xy*o.vertex.w,o.vertex.z,o.vertex.w),_Slider);


				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float rangeInfluence = saturate((1.0 - ( distance(worldPos, _FocusPoint ) / _PerspectiveRemoveRange )));

				float4 rangePositionLerp = lerp(o.vertex,clipPositionLerp,rangeInfluence);
				
				o.vertex = rangePositionLerp;
				//-------------------------------DONE!!!!!-----------------------------------------
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				// I don't care instance!!!!!
				//UNITY_SETUP_INSTANCE_ID(i);
				//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				finalColor = tex2D( _TextureSample0, i.ase_texcoord1.xy);
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18800
7;36;1906;983;1236;231.5;1;True;True
Node;AmplifyShaderEditor.PosVertexDataNode;8;-672,166.5;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PosVertexDataNode;4;-692,-43.5;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TransformPositionNode;9;-488,173.5;Inherit;False;Object;View;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Matrix4X4Node;11;-546,376.5;Inherit;False;Global;PMRemove;PMRemove;0;0;Create;True;0;0;0;True;0;False;1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-141,223.5;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TransformPositionNode;7;-425,-38.5;Inherit;False;Object;Clip;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;12;-6,18.5;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;2;173,169.5;Float;False;myVarName;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;16;17,339.5;Inherit;False;Property;_Slider;Slider;1;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;13;153,-169.5;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;92e82aa8ac3fc5a4aa52c84b6c129b71;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;397,290.5;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;496,6.5;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;731,-55;Float;False;True;-1;2;ASEMaterialInspector;100;1;Project;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque=RenderType;True;2;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;False;0
WireConnection;9;0;8;0
WireConnection;10;0;9;0
WireConnection;10;1;11;0
WireConnection;7;0;4;0
WireConnection;12;0;7;0
WireConnection;12;1;10;0
WireConnection;2;0;12;0
WireConnection;15;0;2;0
WireConnection;15;1;16;0
WireConnection;14;0;13;0
WireConnection;14;1;15;0
WireConnection;1;0;14;0
ASEEND*/
//CHKSM=B9C11BF7B9DD3169431AAF9EE934D5C67C556D7B