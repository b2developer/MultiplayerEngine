Shader "Custom/GradientSkybox"
{
	Properties
	{
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_MiddleColor("Middle Color", Color) = (1,1,1,1)
		_TopColor("Top Color", Color) = (1,1,1,1)
		_Split1("Split 1", Range(0, 1)) = 0.0
		_Split2("Split 2", Range(0, 1)) = 0.0
		_Split3("Split 3", Range(0, 1)) = 0.0
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0

		Pass
		{
			Name "Unlit"
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM

		#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
		//only defining to not throw compilation error over Unity 5.5
		#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
		#endif
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_instancing
		#include "UnityCG.cginc"
		#pragma shader_feature_local _SCREENSPACE_ON


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID

			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
			};

			uniform fixed4 _BottomColor;
			uniform fixed4 _MiddleColor;
			uniform fixed4 _TopColor;
			float _Split1;
			float _Split2;
			float _Split3;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;

				o.ase_texcoord1 = v.vertex;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
#endif
				return o;
			}

			fixed4 gradient(fixed4 c1, fixed4 c2, fixed4 c3, float l1, float l2, float l3, float l)
			{
				if (l < l1)
				{
					fixed4 c = c1;
					return c;
				}
				else if (l >= l1 && l < l2)
				{
					fixed4 c = lerp(c1, c2, (l - l1) / (l2 - l1));
					return c;
				}
				else if (l >= l2 && l < l3)
				{
					fixed4 c = lerp(c2, c3, (l - l2) / (l3 - l2));
					return c;
				}
				else
				{
					fixed4 c = c3;
					return c;
				}
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
#endif
				float4 screenPos = i.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				#ifdef _SCREENSPACE_ON
				float staticSwitch13 = ase_screenPosNorm.y;
				#else
				float staticSwitch13 = i.ase_texcoord1.xyz.y;
				#endif

				float lerp = clamp(staticSwitch13, -1.0f, 1.0f);
				lerp *= 0.5f;
				lerp += 0.5f;

				fixed4 c = gradient(_BottomColor, _MiddleColor, _TopColor, _Split1, _Split2, _Split3, lerp);

				return c;
			}
			ENDCG
		}
	}
}
