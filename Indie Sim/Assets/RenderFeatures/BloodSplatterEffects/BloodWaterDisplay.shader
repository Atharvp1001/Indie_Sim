Shader "Custom/BloodWaterDisplay"
{
    // Unlit, transparent. Draws animated "water" (scrolling caustics) but ONLY
    // where the blood canvas (_MainTex alpha) has been painted. World-space UVs
    // keep the caustics seamless across chunk boundaries. _Tint is driven at
    // runtime by PsychedelicBloodController for the hue shift.
    Properties
    {
        _MainTex ("Blood Canvas (mask)", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)

        _WaterColor ("Water Color", Color) = (0.15, 0.35, 0.55, 0.85)
        _HighlightColor ("Caustic Highlight", Color) = (0.7, 0.95, 1.0, 1.0)

        _CausticTex ("Caustic Texture", 2D) = "white" {}
        _CausticScale ("Caustic World Scale", Float) = 0.15
        _CausticSpeed ("Caustic Speed", Float) = 0.08
        _CausticStrength ("Caustic Strength", Range(0,2)) = 1.0

        _EdgeFade ("Edge Fade (mask softness)", Range(0.001,0.5)) = 0.15
        _MaskCutoff ("Mask Cutoff", Range(0,1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_CausticTex);   SAMPLER(sampler_CausticTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Tint;
                half4  _WaterColor;
                half4  _HighlightColor;
                float  _CausticScale;
                float  _CausticSpeed;
                float  _CausticStrength;
                float  _EdgeFade;
                float  _MaskCutoff;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wpos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(wpos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.worldXY = wpos.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- blood mask ---
                half bloodA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                if (bloodA <= _MaskCutoff)
                    discard;

                half mask = smoothstep(_MaskCutoff, _MaskCutoff + _EdgeFade, bloodA);

                // --- animated caustics (two layers scrolling opposite ways) ---
                float2 uvW = IN.worldXY * _CausticScale;
                float t = _Time.y * _CausticSpeed;

                half c1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uvW + float2(t, t * 0.6)).r;
                half c2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uvW * 1.37 - float2(t * 0.8, t)).r;
                half caustic = saturate(min(c1, c2) * 2.0) * _CausticStrength;

                half3 water = lerp(_WaterColor.rgb, _HighlightColor.rgb, caustic);
                water *= _Tint.rgb;

                half alpha = mask * _WaterColor.a * _Tint.a;
                return half4(water, alpha);
            }
            ENDHLSL
        }
    }
}
