Shader "Custom/WorldSpaceTransparentShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100
            
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Lambert vertex:vert alpha:blend
        #pragma vertex vert alpha
        //#pragma fragment frag alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		float4 _MainTex_ST;

        struct Input
        {
			float3 worldNormal;
			float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
			fixed4 c = fixed4(0, 0, 0, 1);
			fixed4 sc = fixed4(0, 0, 0, 1);

			if (abs(IN.worldNormal.y) > 0.5)
			{
				c = tex2D(_MainTex, IN.worldPos.xz * _MainTex_ST.xy) * _Color;
			}
			else if (abs(IN.worldNormal.x) > 0.5)
			{
				c = tex2D(_MainTex, IN.worldPos.zy * _MainTex_ST.xy) * _Color;
			}
			else
			{
				c = tex2D(_MainTex, IN.worldPos.xy * _MainTex_ST.xy) * _Color;
			}

            // Albedo comes from a texture tinted by color
			o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
