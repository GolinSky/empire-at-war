Shader "Hidden/EmpireAtWar/HazardNoiseBake"
{
    Properties { _Warp("Warp channels", Float) = 0 }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"
            float _Warp;
            // Sine-free hash keeps the pattern stable across GPUs.
            float2 Hash22(float2 p)
            {
                float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy) * 2 - 1;
            }
            float Gradient(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * f * (f * (f * 6 - 15) + 10);
                float a = dot(Hash22(i), f);
                float b = dot(Hash22(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(Hash22(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(Hash22(i + 1), f - 1);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            // Rotating each octave hides the lattice; result is roughly 0..1.
            float Fbm(float2 p, int octaves)
            {
                const float2x2 turn = float2x2(0.8, -0.6, 0.6, 0.8);
                float sum = 0, amplitude = 0.5;
                for (int k = 0; k < octaves; k++)
                {
                    sum += amplitude * Gradient(p);
                    p = mul(turn, p) * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return saturate(sum * 1.1 + 0.5);
            }

            float4 Fields(float2 p)
            {
                if (_Warp > 0.5) return float4(Fbm(p, 3), Fbm(p + float2(5.2, 1.3), 3), 0, 1);
                return float4(Fbm(p, 4), Fbm(p * 3.7 + 11.3, 3), Fbm(p * 1.1, 3), Fbm(p, 2));
            }
            float4 Frag(v2f_img input) : SV_Target
            {
                // Blend translated copies so both value and slope match at every repeat seam.
                float2 p = input.uv * 8;
                float2 blend = input.uv * input.uv * (3 - 2 * input.uv);
                return lerp(lerp(Fields(p), Fields(p - float2(8, 0)), blend.x),
                    lerp(Fields(p - float2(0, 8)), Fields(p - 8), blend.x), blend.y);
            }
            ENDCG
        }
    }
}
