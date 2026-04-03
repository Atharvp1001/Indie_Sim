Shader "Hidden/SplatPainter"
{
    Properties
    {
        _MainTex ("Canvas (Current RT)", 2D) = "black" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _SplatPos ("Splat UV Position", Vector) = (0.5, 0.5, 0, 0)
        _SplatSize ("Splat Size (UV units)", Float) = 0.1
        _Color ("Splat Color", Color) = (1, 0, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _SplatPos;
            float _SplatSize;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample existing canvas
                fixed4 canvas = tex2D(_MainTex, i.uv);
                
                // Calculate distance from splat center in UV space
                float2 dist = (i.uv - _SplatPos.xy) / _SplatSize;
                
                // Sample brush texture (centered at 0.5, 0.5)
                float2 brushUV = dist + 0.5;
                fixed4 brush = tex2D(_BrushTex, brushUV);
                
                // Mask: only apply brush within bounds
                float inBounds = (brushUV.x >= 0 && brushUV.x <= 1 && 
                                  brushUV.y >= 0 && brushUV.y <= 1) ? 1 : 0;
                
                // Combine: add brush with alpha blending
                // Using max() instead of add to prevent yellowing
                fixed4 splat = brush * _Color * inBounds;
                fixed4 result = lerp(canvas, splat, splat.a * inBounds);
                
                return result;
            }
            ENDCG
        }
    }
}
