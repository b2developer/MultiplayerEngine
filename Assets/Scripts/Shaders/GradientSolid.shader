// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/GradientSolid"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _SecondColor("Second Color", Color) = (1,1,1,1)
        _ThirdColor("Third Color", Color) = (1,1,1,1)
        _FourthColor("Fourth Color", Color) = (1,1,1,1)
        _FifthColor("Fifth Color", Color) = (1,1,1,1)
        _SixthColor("Sixth Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MinRange ("MinRange", Float) = 0.0
        _MaxRange ("MaxRange", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 position;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _SecondColor;
        fixed4 _ThirdColor;
        fixed4 _FourthColor;
        fixed4 _FifthColor;
        fixed4 _SixthColor;
        fixed4 _Direction;
        half _MinRange;
        half _MaxRange;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.position = v.vertex;
            //o.direction = v.normal;
        }

        fixed4 gradient(float l)
        {
            float SEVENTH = 0.14285714285f;

            if (l < SEVENTH)
            {
                fixed4 c = _Color;
                return c;
            }
            else if (l < SEVENTH * 2.0f)
            {
                fixed4 c = lerp(_Color, _SecondColor, (l / SEVENTH) - 1.0f);
                return c;
            }
            else if (l < SEVENTH * 3.0f)
            {
                fixed4 c = lerp(_SecondColor, _ThirdColor, (l / SEVENTH) - 2.0f);
                return c;
            }
            else if (l < SEVENTH * 4.0f)
            {
                fixed4 c = lerp(_ThirdColor, _FourthColor, (l / SEVENTH) - 3.0f);
                return c;
            }
            else if (l < SEVENTH * 5.0f)
            {
                fixed4 c = lerp(_FourthColor, _FifthColor, (l / SEVENTH) - 4.0f);
                return c;
            }
            else if (l < SEVENTH * 6.0f)
            {
                fixed4 c = lerp(_FifthColor, _SixthColor, (l / SEVENTH) - 5.0f);
                return c;
            }
            else
            {
                fixed4 c = _SixthColor;
                return c;
            }
        }

        float modc(float a, float b)
        {
            return a - (b * floor(a / b));
        }

        fixed3 rgbtohsv(float r, float g, float b)
        {
            float cmax = max(r, max(g, b));
            float cmin = min(r, min(g, b));

            float d = cmax - cmin;

            float hue = 0.0f;

            if (cmax == r)
            {
                hue = 60.0f * modc((g - b) / d, 6.0f);
            }
            else if (cmax == g)
            {
                hue = 60.0f * (((b - r) / d) + 2.0f);
            }
            else if (cmax == b)
            {
                hue = 60.0f * (((r - g) / d) + 4.0f);
            }

            float saturation = 0.0f;

            if (cmax != 0.0f)
            {
                saturation = d / cmax;
            }

            float value = cmax;

            fixed3 hsv = fixed3(hue, saturation, value);
            return hsv;
        }

        fixed3 hsvtorgb(float h, float s, float v)
        {
            float c = v * s;

            float x = c * (1.0f - abs(modc(h / 60.0f, 2.0f) - 1.0f));
            float m = v - c;

            fixed3 col = fixed3(0.0f, 0.0f, 0.0f);

            if (h < 60.0f)
            {
                col = fixed3(c, x, 0.0f);
            }
            else if (h < 120.0f)
            {
                col = fixed3(x, c, 0.0f);
            }
            else if (h < 180.0f)
            {
                col = fixed3(0.0f, c, x);
            }
            else if (h < 240.0f)
            {
                col = fixed3(0.0f, x, c);
            }
            else if (h < 300.0f)
            {
                col = fixed3(x, 0.0f, c);
            }
            else if (h < 360.0f)
            {
                col = fixed3(c, 0.0f, x);
            }

            fixed3 rgb = fixed3(col.r + m, col.g + m, col.b + m);
            return rgb;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float mapped = (IN.worldPos.y - _MinRange) / (_MaxRange - _MinRange);

            fixed4 rgb = gradient(mapped);
            fixed3 rgb3 = fixed3(rgb.r, rgb.g, rgb.b);
            fixed3 hsv = rgbtohsv(rgb3.r, rgb3.g, rgb3.b);
            //hsv.g = 1.0f;
            //hsv.b = 1.0f;

            fixed3 f = hsvtorgb(hsv.r, hsv.g, hsv.b);

            // Albedo comes from a texture tinted by color
            fixed4 c = fixed4(f.r, f.g, f.b, 1.0f);
            c = fixed4(rgb.r, rgb.g, rgb.b, 1.0f);

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
