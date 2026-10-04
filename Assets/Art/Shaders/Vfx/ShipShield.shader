Shader "EmpireAtWar/Ship Shield"
{
    Properties
    {
        [HDR] _ShieldColor ("Color", Color) = (0.15, 0.65, 1, 0.65)
        _Brightness ("Brightness", Float) = 2
        _FadeDuration ("Fade Duration", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ShieldImpact"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_IMPACTS 8
            // Ring width, and how far the ring travels before the impact fades, as fractions of the impact radius.
            #define WAVE_WIDTH_RATIO 0.3
            #define WAVE_REACH 1.5
            CBUFFER_START(UnityPerMaterial)
                half4 _ShieldColor;
                float _Brightness, _FadeDuration;
            CBUFFER_END
            // xyz: object-space hit point on the shell, w: hit time.
            float4 _Impacts[MAX_IMPACTS];
            // World-unit radius per impact, set from the hit's damage, so equal hits look equal on any shield size.
            float _ImpactRadii[MAX_IMPACTS];
            float _DisplacementRatio, _MaxDisplacement;
            int _ImpactCount;
            float _ShieldTime;
            float3 _ShieldAxes;

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 surfaceOS : TEXCOORD0; };

            void EvaluateImpact(float3 surface, int index, out float patch, out float wave, out float fade)
            {
                float age = max(0.0, _ShieldTime - _Impacts[index].w);
                float duration = max(0.001, _FadeDuration);
                fade = (1.0 - smoothstep(0.0, duration, age)) * smoothstep(0.0, min(0.06, duration * 0.1), age);
                float radius = max(0.001, _ImpactRadii[index]);
                // Straight-line distance in world units; the shell is tight, so it stays close to the surface path.
                float distance = length((surface - _Impacts[index].xyz) * _ShieldAxes);
                patch = 1.0 - smoothstep(0.0, radius, distance);
                // The ring keeps full strength across the patch and dies out by WAVE_REACH radii.
                float phase = (distance - age * radius * WAVE_REACH / duration) / (radius * WAVE_WIDTH_RATIO);
                float envelope = 1.0 - smoothstep(0.0, 1.0, abs(phase));
                float reach = 1.0 - smoothstep(radius, radius * WAVE_REACH, distance);
                wave = sin(phase * TWO_PI) * envelope * reach;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.surfaceOS = input.positionOS;
                float displacement = 0.0;
                for (int i = 0; i < _ImpactCount; i++)
                {
                    float patch, wave, fade;
                    EvaluateImpact(output.surfaceOS, i, patch, wave, fade);
                    displacement += wave * fade * _ImpactRadii[i] * _DisplacementRatio;
                }
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                positionWS += TransformObjectToWorldNormal(input.normalOS) * clamp(displacement, -_MaxDisplacement, _MaxDisplacement);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float visibility = 0.0;
                float3 surface = input.surfaceOS;
                for (int i = 0; i < _ImpactCount; i++)
                {
                    float patch, wave, fade;
                    EvaluateImpact(surface, i, patch, wave, fade);
                    visibility += (patch + abs(wave)) * fade;
                }
                return half4(_ShieldColor.rgb * _Brightness, saturate(visibility * _ShieldColor.a));
            }
            ENDHLSL
        }
    }
}
