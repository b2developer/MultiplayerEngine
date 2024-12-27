Shader "Custom/Stars"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;

                fixed3 norm = fixed3(i.vertex.x, i.vertex.y, i.vertex.z);
                float mag = sqrt(norm.x * norm.x + norm.y * norm.y + norm.z * norm.z);

                norm /= mag;

                if (abs(norm.y) > 0.5)
                {
                    col = tex2D(_MainTex, i.vertex.xz * _MainTex_ST.xy) * _Color;
                }
                else if (abs(norm.x) > 0.5)
                {
                    col = tex2D(_MainTex, i.vertex.zy * _MainTex_ST.xy) * _Color;
                }
                else
                {
                    col = tex2D(_MainTex, i.vertex.xy * _MainTex_ST.xy) * _Color;
                }

                col = tex2D(_MainTex, i.uv) * _Color;

                //col.a += 0.5f;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
