Shader "Custom/Stars"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _N("N", Range(0,1000)) = 250
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
        float4 _Color;
        
        half _Glossiness;
        half _Metallic;
        
        half _N;

        struct Input
        {
            float3 worldNormal;
            float3 worldPos;
        };

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

        fixed3 goldenSpiral(float i)
        {
            fixed3 p = fixed3(0, 0, 0);

            float gr = 1.61803398875f;
            float pi = 3.14159265359f;

            float theta = (2 * pi * i) / gr;
            float phi = acos(1.0f - ((2 * i) / _N));

            p.x = cos(theta) * sin(phi);
            p.y = sin(theta) * sin(phi);
            p.z = cos(phi);

            return p;
        }

        float distance(fixed3 a, fixed3 b)
        {
            fixed3 r = fixed3(b.x - a.x, b.y - a.y, b.z - a.z);
            float mag = sqrt(r.x * r.x + r.y * r.y + r.z * r.z);
            return mag;
        }

        float distance(fixed3 v)
        {
            float mag = sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return mag;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = fixed4(0, 0, 0, 0);
            fixed4 c2 = fixed4(1, 1, 1, 1);

            fixed3 n = fixed3(IN.worldPos.x, IN.worldPos.y, IN.worldPos.z);
            float m = distance(n);
            fixed3 nn = fixed3(n.x / m, n.y / m, n.z / m);

            float z = IN.worldNormal.z;
            float ip = _N * (-z + 1.0f) * 0.5f;

            float ifv = floor(ip);
            float icv = ceil(ip);

            fixed3 gsf = goldenSpiral(ifv);
            fixed3 gcf = goldenSpiral(icv);

            float df = distance(gsf, nn);
            float dc = distance(gcf, nn);

            float dist = min(df, dc);

            float lf = 0.0f;

            if (dist < 0.9f)
            {
                lf = 1.0f;
            }

            fixed4 l = lerp(c, c2, lf);

            // Albedo comes from a texture tinted by color
            o.Albedo = l.rgb;
            o.Alpha = l.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
