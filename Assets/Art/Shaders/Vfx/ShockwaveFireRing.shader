Shader "EmpireAtWar/Vfx/Shockwave Fire Ring"
{
    // A flat ring of fire on a quad in the XZ plane. ShockwaveView scales it with the refraction sphere and drives
    // _Progress. Queue 2998 keeps the death fireball (2999) drawn on top of it.
    Properties
    {
        // Ring center and half-width, as fractions of the quad's half-size.
        _RingRadius ("Ring Radius", Range(0.1, 1)) = 0.74
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.16
        // How far billows push the ring in and out, as a fraction of the quad's half-size.
        _Wobble ("Edge Wobble", Range(0, 0.3)) = 0.08
        _NoiseScale ("Noise Scale", Range(1, 16)) = 5
        // How far the fire texture streams outward over the ring's life.
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 3
        [HDR] _HotColor ("Hot Color", Color) = (6, 5, 3.6, 1)
        [HDR] _FireColor ("Fire Color", Color) = (3, 1.1, 0.3, 1)
        _SmokeColor ("Smoke Color", Color) = (0.12, 0.07, 0.06, 1)
        // 0..1 age of the wave; set per instance by ShockwaveView.
        _Progress ("Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-2" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "FireRing"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            // Premultiplied: fire adds light, smoke darkens what is behind it.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShockwaveNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _RingRadius, _RingWidth, _Wobble, _NoiseScale, _FlowSpeed, _Progress;
                half4 _HotColor, _FireColor, _SmokeColor;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 planar = (input.uv - 0.5) * 2.0;
                float radius = length(planar);
                // Noise is sampled on the unit circle's direction, so there is no seam where the angle wraps.
                float2 direction = planar / max(radius, 1e-4);

                float wobble = (Fbm(float3(direction * _NoiseScale, _Progress * 2.0 + _Time.y * 0.2)) - 0.5) * 2.0 * _Wobble;
                // -1 at the inner edge, 0 at the ring's center line, +1 at the leading edge.
                float across = (radius - _RingRadius - wobble) / _RingWidth;
                // A sharp leading edge and a longer, softer trail behind it.
                float profile = across > 0.0 ? 1.0 - smoothstep(0.0, 1.0, across) : 1.0 - smoothstep(0.0, 1.6, -across);

                // Billowing fire that streams outward with the wave.
                float billow = Fbm(float3(direction * _NoiseScale * 2.0, radius * _NoiseScale * 1.5 - _Progress * _FlowSpeed));
                float density = saturate(profile * (0.35 + 1.1 * billow));
                // Hottest at the leading edge and early on; cools to smoke as it ages.
                float heat = saturate(density * (1.3 - _Progress) * (0.6 + 0.4 * saturate(across + 0.5)));
                half3 color = heat < 0.5
                    ? lerp(_SmokeColor.rgb, _FireColor.rgb, heat * 2.0)
                    : lerp(_FireColor.rgb, _HotColor.rgb, heat * 2.0 - 1.0);

                float fade = smoothstep(0.0, 0.04, _Progress) * (1.0 - smoothstep(0.5, 1.0, _Progress));
                float alpha = density * fade;
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
