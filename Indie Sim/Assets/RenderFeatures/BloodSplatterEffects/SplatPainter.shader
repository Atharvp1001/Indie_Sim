Shader "Unlit/SplatPainter"
{
    Properties {
        _MainTex ("Canvas", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _SplatPos ("Splat Position", Vector) = (0,0,0,0)
        _SplatSize ("Splat Size", Float) = 0.05
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _SplatPos;
            float _SplatSize;
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 canvas = tex2D(_MainTex, i.uv);
                // Calculate distance from current pixel to the splatter impact point
                float2 dist = (i.uv - _SplatPos.xy) / _SplatSize;
                // Center the brush
                fixed4 brush = tex2D(_BrushTex, dist + 0.5);
                
                // Mask out the brush if it's outside the "stamp" area
                if (dist.x < -0.5 || dist.x > 0.5 || dist.y < -0.5 || dist.y > 0.5) {
                    brush = 0;
                }

                // Add the new blood to the existing canvas
                return saturate(canvas + (brush * _Color));
            }
            ENDCG
        }
    }
}