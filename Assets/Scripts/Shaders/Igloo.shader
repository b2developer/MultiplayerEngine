Shader "Custom/Igloo"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _SecondColor("Second Color", Color) = (1,1,1,1)
        _ThirdColor("Third Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Splits("Splits", Float) = 0.0
        _Splits2("Splits2", Float) = 0.0
        _EvenBullshit("Even Bullshit", Float) = 0.0
        _BrickShift("Brick Shift", Float) = 0.0
        _SwirlStrength("SwirlStrength", Float) = 1.0
        _Cutoff("Cutoff", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 2.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 position: SV_POSITION;
            //float3 direction;
            //float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _SecondColor;
        fixed4 _ThirdColor;
        half _Splits;
        half _Splits2;
        half _EvenBullshit;
        half _BrickShift;
        half _SwirlStrength;
        half _Cutoff;

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

        float modc(float a, float b)
        {
            return a - (b * floor(a / b));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float mag = sqrt(IN.position.x * IN.position.x + IN.position.y * IN.position.y + IN.position.z * IN.position.z);
            fixed3 v = fixed3(IN.position.x / mag, IN.position.y / mag, IN.position.z / mag);

            float length = sqrt(v.z * v.z + v.x * v.x);

            fixed2 v2 = fixed2(length, v.y);
            float mag3 = sqrt(v2.x * v2.x + v2.y * v2.y);

            fixed2 v3 = fixed2(v2.x / mag3, v2.y / mag3);

            float angle2 = atan2(v3.y, v3.x);

            fixed4 c = _Color;

            if (angle2 > _Cutoff)
            {
                c = _Color;
            }
            else
            {
                angle2 = (angle2 + 6.28318530718f) % 6.28318530718f;

                float angle = atan2(v.z, v.x);
                angle = (angle + 6.28318530718f) % 6.28318530718f;

                float divisor = angle2 / _EvenBullshit;
                int integer = ceil(divisor);

                float offset = 0.0f;

                if (integer % 2 > 0)
                {
                    offset = _BrickShift;
                }

                angle += offset;

                //angle += _SwirlStrength * length;

                float split = angle % _Splits;
                float split2 = angle2 % _Splits2;

                // Albedo comes from a texture tinted by color
                c = lerp(_Color, _ThirdColor, 1.0f - v.y);

                if (split > _Splits * 0.98f || split2 > _Splits2 * 0.98f)
                {
                    c = _SecondColor;
                }

            }

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
