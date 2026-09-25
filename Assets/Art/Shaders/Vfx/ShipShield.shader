Shader "EmpireAtWar/Ship Shield"
{
    Properties
    {
        [HDR] _ShieldColor ("Color", Color) = (0.15, 0.65, 1, 0.65)
        _Brightness ("Brightness", Float) = 2
        _VisibilityRadius ("Surface Visibility Radius", Float) = 4
        _FadeDuration ("Fade Duration", Float) = 1.2
        _WaveSpeed ("Surface Wave Speed", Float) = 6
        _WaveWidth ("Wave Width", Float) = 0.8
        _DisplacementStrength ("Vertex Displacement", Float) = 0.15
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
            CBUFFER_START(UnityPerMaterial)
                half4 _ShieldColor;
                float _Brightness, _VisibilityRadius, _FadeDuration;
                float _WaveSpeed, _WaveWidth, _DisplacementStrength;
            CBUFFER_END
            float4 _Impacts[MAX_IMPACTS];
            int _ImpactCount;
            float _ShieldTime;
            float3 _ShieldAxes;

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 surfaceOS : TEXCOORD0; };

            float SurfaceDistance(float3 a, float3 b)
            {
                float cosine = clamp(dot(a, b), -1.0, 1.0);
                float angle = acos(cosine);
                float3 tangent = b - a * cosine;
                float sine = length(tangent);
                if (sine < 0.0001)
                    tangent = normalize(cross(a, abs(a.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0)));
                else
                    tangent /= sine;
                float3 endTangent = tangent * cosine - a * sine;
                float3 midTangent = tangent * cos(angle * 0.5) - a * sin(angle * 0.5);
                // Arc length along the stretched sphere (Simpson integration), never through its interior.
                return angle * (length(tangent * _ShieldAxes) + 4.0 * length(midTangent * _ShieldAxes)
                    + length(endTangent * _ShieldAxes)) / 6.0;
            }

            void EvaluateImpact(float3 surface, int index, out float patch, out float wave, out float fade)
            {
                float age = max(0.0, _ShieldTime - _Impacts[index].w);
                float duration = max(0.001, _FadeDuration);
                fade = (1.0 - smoothstep(0.0, duration, age)) * smoothstep(0.0, min(0.06, duration * 0.1), age);
                float distance = SurfaceDistance(_Impacts[index].xyz, surface);
                patch = 1.0 - smoothstep(0.0, max(0.001, _VisibilityRadius), distance);
                float phase = (distance - age * _WaveSpeed) / max(0.001, _WaveWidth);
                float envelope = 1.0 - smoothstep(0.0, 1.0, abs(phase));
                wave = sin(phase * TWO_PI) * envelope;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.surfaceOS = normalize(input.positionOS);
                float displacement = 0.0;
                for (int i = 0; i < _ImpactCount; i++)
                {
                    float patch, wave, fade;
                    EvaluateImpact(output.surfaceOS, i, patch, wave, fade);
                    displacement += wave * fade;
                }
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                positionWS += TransformObjectToWorldNormal(input.normalOS) * clamp(displacement, -1.0, 1.0) * _DisplacementStrength;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float visibility = 0.0;
                float3 surface = normalize(input.surfaceOS);
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
