Shader "EmpireAtWar/SpaceHazard"
{
    Properties
    {
        _BaseColor("Core", Color) = (0.525,0.345,0.722,0.35)
        _AccentColor("Edge and highlights", Color) = (0.69,0.31,0.58,1)
        _Ion("Electrical storm", Float) = 0
        _Speed("Drift", Float) = 0.01
        _Feather("Edge feather (m)", Float) = 24
        _WispReach("Wisp reach outside the edge (m)", Float) = 90
        _Stretch("Streak stretch along the flow", Float) = 0.4
        [NoScaleOffset] _Distance("Signed distance (m)", 2D) = "white" {}
        _FlowDirection("Flow direction (xz)", Vector) = (1,0,0,0)
        _NoiseScale("Noise frequency (1/m)", Float) = 0.004
        _LayerOpacity("Layer opacity", Float) = 1
        _NoiseOffset("Layer offset", Float) = 0
        _Inset("Layer inset (m)", Float) = 0
        _Softness("Layer edge softness", Float) = 1
        _Flash("Flash (x, z, radius, strength)", Vector) = (0,0,1,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-50" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            TEXTURE2D(_Distance);
            SAMPLER(sampler_Distance);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _AccentColor;
                float4 _FlowDirection, _Flash;
                float _Ion, _Speed, _Feather, _WispReach, _Stretch, _NoiseScale;
                float _LayerOpacity, _NoiseOffset, _Inset, _Softness;
            CBUFFER_END

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
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // The distance texture is the gameplay boundary: the body ends exactly at sdf = 0.
                float sdf = SAMPLE_TEXTURE2D(_Distance, sampler_Distance, input.uv).r + _Inset;
                if (sdf > _WispReach) discard;
                float2 flow = normalize(_FlowDirection.xy);
                float2 p = input.world.xz;
                float2 local = float2(dot(p, flow), dot(p, float2(-flow.y, flow.x)));
                float2 s = local * _NoiseScale * float2(_Stretch, 1) + _NoiseOffset;
                float t = _Time.y * _Speed;
                // Domain warp curls the streaks into filaments that drift along the stream.
                float2 warp = float2(Fbm(s + float2(t, 0), 3), Fbm(s + float2(5.2, 1.3) - t * 0.6, 3)) - 0.5;
                float2 w = s + warp * 1.8;
                float n = Fbm(w + float2(t * 0.5, 0), 4);
                float fine = Fbm(w * 3.7 + 11.3, 3);

                float feather = _Feather * _Softness * (0.4 + n * 1.2);
                float body = saturate(-sdf / feather);
                body = body * body * (3 - 2 * body);
                float depth = saturate(-sdf / (_Feather * 6));
                float wisp = sdf > 0 ? (1 - sdf / _WispReach) * smoothstep(0.6, 0.85, fine) * 0.15 : 0;
                // Strong contrast leaves thin and dense pockets instead of an even wash.
                float density = saturate(n * 1.6 - 0.35 + (fine - 0.5) * 0.7) * lerp(0.55, 1, depth);

                half3 col = lerp(_AccentColor.rgb, _BaseColor.rgb, depth * 0.7) * (0.45 + density * 0.9);
                // Ridged noise peaks on thin lines: crackling filaments in an ion storm.
                float ridge = 1 - abs(Fbm(w * 1.1 + t * 2, 3) * 2 - 1);
                float filament = pow(saturate(ridge), 18) * smoothstep(0.45, 0.75, fine) * _Ion *
                    (0.6 + 0.4 * sin(_Time.y * 3 + n * 20)) * 2;
                float2 toFlash = p - _Flash.xy;
                float flash = _Flash.w * exp(-dot(toFlash, toFlash) / (_Flash.z * _Flash.z));
                float sparkle = pow(saturate(Fbm(p * 0.09, 2)), 14) * (1 - _Ion) * 1.5;
                col += _AccentColor.rgb * (filament * 1.4 + flash) + sparkle;

                // The floor keeps the gameplay edge readable even where the noise thins out.
                float alpha = body * (0.25 + 0.75 * density) + wisp * (1 - body) + (filament * 0.4 + flash * 0.25) * body;
                return half4(col, saturate(alpha * _BaseColor.a * _LayerOpacity));
            }
            ENDHLSL
        }
    }
}
