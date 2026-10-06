Shader "EmpireAtWar/Planet Atmosphere"
{
    Properties
    {
        _InnerHazeStrength ("Inner Haze Strength", Range(0, 1)) = 0.12
        [HDR] _RimColor ("Rim Color", Color) = (0.3, 0.7, 1, 1)
        _RimWidth ("Rim Width (Planet Radius)", Range(0.002, 0.08)) = 0.015
        [HDR] _HaloColor ("Outer Halo Color", Color) = (0.035, 0.18, 0.65, 1)
        _HaloThickness ("Outer Halo Thickness (Planet Radius)", Range(0.005, 0.08)) = 0.045
        _Brightness ("Brightness", Range(0, 3)) = 1
        _FadeSoftness ("Outward Fade Softness", Range(1, 6)) = 2
        _NightVisibility ("Night Side Visibility", Range(0, 0.5)) = 0.06
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-420" "RenderType" = "Transparent" }
        Pass
        {
            Name "Atmosphere"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half4 _HaloColor;
                float _InnerHazeStrength;
                float _RimWidth;
                float _HaloThickness;
                float _Brightness;
                float _FadeSoftness;
                float _NightVisibility;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Unit-radius shell, centered on the planet and scaled to 1.12 times its radius.
                // The extra margin keeps the transparent tail inside the polygon silhouette.
                const float SHELL_SCALE = 1.12;
                float3 position = TransformWorldToObject(input.positionWS) * SHELL_SCALE;
                float3 toCamera = normalize(mul((float3x3)GetWorldToObjectMatrix(),
                    GetWorldSpaceNormalizeViewDir(input.positionWS)));
                float3 closest = position - toCamera * dot(position, toCamera);
                float radius = length(closest);
                float outside = saturate((radius - 1.0) / _HaloThickness);
                float fade = pow(saturate(1.0 - smoothstep(0.0, 1.0, outside)), _FadeSoftness);
                float haze = _InnerHazeStrength * smoothstep(0.88, 1.0, radius) * fade;
                float rimDistance = (radius - 1.0) / _RimWidth;
                float rim = 0.6 * exp2(-rimDistance * rimDistance) * fade;
                float halo = 0.25 * smoothstep(0.96, 1.0, radius) * fade;

                // Use the front surface on the disk, and the tangent point beyond its edge.
                float3 litPosition = closest + toCamera * sqrt(saturate(1.0 - radius * radius));
                float3 normalWS = TransformObjectToWorldNormal(normalize(litPosition));
                Light sun = GetMainLight();
                float day = smoothstep(-0.25, 0.6, dot(normalWS, sun.direction));
                float illumination = lerp(_NightVisibility, 1.0, day);
                float density = haze + rim + halo;
                half3 color = (_HaloColor.rgb * (haze + halo) + _RimColor.rgb * rim)
                    / max(density, 0.0001);
                color = lerp(color, lerp(color, half3(1, 1, 1), 0.25), day);
                color *= lerp(half3(0.3, 0.45, 0.8), sun.color, day);
                half alpha = 1.0 - exp(-density * illumination * _Brightness);
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
