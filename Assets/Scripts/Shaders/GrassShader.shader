Shader "Custom/GrassShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _SecondColor("Second Color", Color) = (1,1,1,1)
        _SecondTex("Second Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Lerp("Lerp", Range(0,1)) = 0.0
        _SecondLerp("Second Lerp", Range(0,1)) = 0.0
        _Blend("Blend", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _MainTex_ST;

        sampler2D _SecondTex;
        float4 _SecondTex_ST;

        struct Input
        {
            float3 worldNormal;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;

        fixed4 _Color;
        fixed4 _SecondColor;

        half _Lerp;
        half _SecondLerp;
        half _Blend;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = fixed4(0, 0, 0, 1);

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

            c = lerp(c, _Color, _Lerp);

            fixed4 c2 = fixed4(0, 0, 0, 1);

            if (abs(IN.worldNormal.y) > 0.5)
            {
                c2 = tex2D(_SecondTex, IN.worldPos.xz * _SecondTex_ST.xy) * _SecondColor;
            }
            else if (abs(IN.worldNormal.x) > 0.5)
            {
                c2 = tex2D(_SecondTex, IN.worldPos.zy * _SecondTex_ST.xy) * _SecondColor;
            }
            else
            {
                c2 = tex2D(_SecondTex, IN.worldPos.xy * _SecondTex_ST.xy) * _SecondColor;
            }

            c2 = lerp(c2, _SecondColor, _SecondLerp);

            fixed4 b = fixed4(0, 0, 0, 1);
            b = lerp(c, c2, _Blend);

            // Albedo comes from a texture tinted by color
            o.Albedo = b.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = b.a;
        }
        ENDCG
    }
        FallBack "Diffuse"
}
