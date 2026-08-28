Shader "Hidden/TrippyBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _DarkColor;
            float4 _MidColor;
            float4 _LightColor;
            int _SectionsX;
            int _SectionsY;
            float _BlendRadius;
            float _Time;
            float _StrobeSpeed;
            float _PhaseOffset;
            
            float _SectionPhases[9];
            float _SectionSpeeds[9];
            float _SectionStrobes[9];
            
            float smoothDistance(float2 uv, float2 center, float radius)
            {
                float dist = distance(uv, center);
                return smoothstep(radius, radius * (1.0 + _BlendRadius), dist);
            }
            
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float4 finalColor = _DarkColor;
                float totalWeight = 0.0;
                
                // Calculate section size
                float sectionWidth = 1.0 / _SectionsX;
                float sectionHeight = 1.0 / _SectionsY;
                
                // Sample and blend from all sections
                for (int x = 0; x < _SectionsX; x++)
                {
                    for (int y = 0; y < _SectionsY; y++)
                    {
                        int idx = y * _SectionsX + x;
                        
                        // Section center
                        float2 sectionCenter = float2(
                            (x + 0.5) * sectionWidth,
                            (y + 0.5) * sectionHeight
                        );
                        
                        // Distance-based weight for blending
                        float dist = distance(uv, sectionCenter);
                        float maxDist = length(float2(sectionWidth, sectionHeight)) * 0.5;
                        float weight = 1.0 - smoothstep(0.0, maxDist * (1.0 + _BlendRadius), dist);
                        
                        if (weight > 0.01)
                        {
                            float phase = _SectionPhases[idx];
                            float speed = _SectionSpeeds[idx];
                            float isStrobe = _SectionStrobes[idx];
                            
                            float value;
                            
                            if (isStrobe > 0.5)
                            {
                                // Strobe pattern
                                float strobeTime = _Time * _StrobeSpeed + phase;
                                value = step(0.5, frac(strobeTime));
                            }
                            else
                            {
                                // Wave pattern
                                float wave = sin(_Time * speed + phase + _PhaseOffset * idx) * 0.5 + 0.5;
                                
                                // Add some noise for organic feel
                                float noise = frac(sin(dot(uv + _Time * 0.1, float2(12.9898, 78.233))) * 43758.5453);
                                wave = lerp(wave, noise, 0.1);
                                
                                value = wave;
                            }
                            
                            // Tri-color interpolation (dark -> mid -> light)
                            float4 sectionColor;
                            if (value < 0.5)
                            {
                                sectionColor = lerp(_DarkColor, _MidColor, value * 2.0);
                            }
                            else
                            {
                                sectionColor = lerp(_MidColor, _LightColor, (value - 0.5) * 2.0);
                            }
                            
                            finalColor += sectionColor * weight;
                            totalWeight += weight;
                        }
                    }
                }
                
                // Normalize by total weight for proper blending
                if (totalWeight > 0.0)
                {
                    finalColor /= totalWeight;
                }
                
                return finalColor;
            }
            ENDPROGRAM
        }
    }
}