Shader "EmpireAtWar/Vfx/Nebula Cloud Volume"
{
    Properties
    {
        _NoiseTex ("Cloud Density (3D)", 3D) = "gray" {}
        _ShadowColor ("Deep Dust", Color) = (0.055, 0.065, 0.11, 1)
        _LightColor ("Soft Blue Light", Color) = (0.30, 0.39, 0.47, 1)
        _AccentColor ("Violet Wisps", Color) = (0.38, 0.24, 0.39, 1)
        _Density ("Cloud Density", Range(0, 20)) = 9
        _Brightness ("Background Brightness", Range(0, 2)) = 0.8
        _DriftSpeed ("Internal Drift", Range(0, 0.05)) = 0.003
        _Steps ("Volume Samples", Range(24, 96)) = 64
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Pass
        {
            Name "NebulaVolume"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE3D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor, _LightColor, _AccentColor;
                float _Density, _Brightness, _DriftSpeed, _Steps;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                // Keep the exit surface available when the camera is inside a large volume.
                #if UNITY_REVERSED_Z
                    output.positionCS.z = max(output.positionCS.z, 0.00001 * output.positionCS.w);
                #else
                    output.positionCS.z = min(output.positionCS.z, 0.99999 * output.positionCS.w);
                #endif
                return output;
            }

            float CloudDensity(float3 p)
            {
                float3 drift = _Time.y * _DriftSpeed * float3(0.24, -0.12, 0.16);
                float3 warp = SAMPLE_TEXTURE3D_LOD(_NoiseTex, sampler_NoiseTex, p * 1.2 + drift, 0).rgb - 0.5;
                float3 q = p + warp * 0.19;
                // Interlocking lobes and a crooked dust lane make a coherent, irregular cloud bank.
                float3 a = (q - float3(-0.22, 0.01, -0.10)) / float3(0.29, 0.32, 0.26);
                float3 b = (q - float3(0.05, -0.06, 0.07)) / float3(0.33, 0.27, 0.27);
                float3 c = (q - float3(0.29, 0.07, 0.19)) / float3(0.20, 0.29, 0.25);
                float envelope = max(max(1.0 - dot(a, a), 1.0 - dot(b, b)), 1.0 - dot(c, c));
                float3 noise = SAMPLE_TEXTURE3D_LOD(_NoiseTex, sampler_NoiseTex, q * 2.7 + drift, 0).rgb;
                float structure = noise.r * 0.67 + noise.g * 0.23 + noise.b * 0.10;
                float clouds = smoothstep(0.32, 0.66, structure + envelope * 0.18);
                float lane = q.z - q.x * 0.38 - sin(q.x * 12.0) * 0.045;
                float dust = lerp(0.28, 1.0, smoothstep(0.012, 0.085, abs(lane)));
                float boundary = saturate((0.5 - max(max(abs(p.x), abs(p.y)), abs(p.z))) * 18.0);
                return clouds * saturate(envelope * 2.5) * dust * boundary;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 directionWS = -GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 originWS = GetCameraPositionWS();
                if (unity_OrthoParams.w > 0.5)
                    originWS = input.positionWS - directionWS * dot(input.positionWS - originWS, directionWS);
                float3 origin = TransformWorldToObject(originWS);
                float3 localDirection = TransformWorldToObjectDir(directionWS, false);
                float worldToLocal = length(localDirection);
                float3 direction = localDirection / worldToLocal;
                bool inside = all(abs(origin) < 0.5);
                bool front = IS_FRONT_VFACE(face, true, false);
                if (front == inside) discard;

                float3 safeDirection = lerp(-1.0, 1.0, step(0.0, direction)) * max(abs(direction), 0.00001);
                float3 t0 = (-0.5 - origin) / safeDirection;
                float3 t1 = (0.5 - origin) / safeDirection;
                float3 nearPlane = min(t0, t1);
                float3 farPlane = max(t0, t1);
                float entry = max(0.0, max(max(nearPlane.x, nearPlane.y), nearPlane.z));
                float exit = min(min(farPlane.x, farPlane.y), farPlane.z);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = SampleSceneDepth(screenUV);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif
                float3 sceneWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                exit = min(exit, dot(sceneWS - originWS, directionWS) * worldToLocal);
                if (exit <= entry) discard;

                int steps = clamp((int)_Steps, 24, 96);
                float stepLength = (exit - entry) / steps;
                float jitter = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                float3 p = origin + direction * (entry + stepLength * lerp(0.25, 0.75, jitter));
                float3 lightDirection = normalize(float3(-0.45, 0.8, -0.3));
                float transmittance = 1.0;
                float3 radiance = 0.0;
                [loop]
                for (int i = 0; i < steps; i++)
                {
                    float density = CloudDensity(p);
                    if (density > 0.002)
                    {
                        float shadow = CloudDensity(p + lightDirection * 0.075);
                        float light = exp(-shadow * 3.5);
                        float violet = smoothstep(-0.25, 0.30, p.x + p.z * 0.5);
                        float3 tint = lerp(_LightColor.rgb, _AccentColor.rgb, violet * 0.70);
                        float3 color = lerp(_ShadowColor.rgb, tint, 0.22 + light * 0.78) * _Brightness;
                        float opacity = 1.0 - exp(-density * _Density * stepLength);
                        radiance += transmittance * opacity * color;
                        transmittance *= 1.0 - opacity;
                        if (transmittance < 0.025) break;
                    }
                    p += direction * stepLength;
                }
                return half4(radiance, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }
}
