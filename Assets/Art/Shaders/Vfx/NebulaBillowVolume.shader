Shader "EmpireAtWar/Vfx/Nebula Billow Volume"
{
    Properties
    {
        _CloudField ("Cloud Shape and Light Transport (3D)", 3D) = "black" {}
        _BodyColor ("Cloud Blue", Color) = (0.35, 0.58, 0.88, 1)
        _PearlColor ("Illuminated Edges", Color) = (0.74, 0.9, 1, 1)
        _ShadowColor ("Interior Shadow", Color) = (0.09, 0.12, 0.23, 1)
        _VioletColor ("Violet Undertones", Color) = (0.6, 0.34, 0.7, 1)
        _Brightness ("Brightness", Range(0, 3)) = 1
        _Extinction ("Cloud Thickness", Range(1, 30)) = 16
        _LightAbsorption ("Self Shadow Depth", Range(1, 30)) = 14
        _Ambient ("Interior Fill Light", Range(0, 1)) = 0.2
        _Flow ("Billow Motion", Range(0, 0.1)) = 0.018
        _Samples ("Ray Samples", Range(32, 128)) = 80
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Name "CloudScattering"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VolumeVertex
            #pragma fragment VolumeFragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE3D(_CloudField);
            SAMPLER(sampler_CloudField);
            CBUFFER_START(UnityPerMaterial)
                float4 _BodyColor, _PearlColor, _ShadowColor, _VioletColor;
                float _Brightness, _Extinction, _LightAbsorption, _Ambient, _Flow, _Samples;
            CBUFFER_END

            struct MeshInput
            {
                float3 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct VolumeInput
            {
                float4 screen : SV_POSITION;
                float3 surface : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VolumeInput VolumeVertex(MeshInput mesh)
            {
                VolumeInput volume;
                UNITY_SETUP_INSTANCE_ID(mesh);
                UNITY_TRANSFER_INSTANCE_ID(mesh, volume);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(volume);
                volume.surface = TransformObjectToWorld(mesh.vertex);
                volume.screen = TransformWorldToHClip(volume.surface);
                // Preserve back faces beyond the far clip plane for cameras inside a cloud.
                #if UNITY_REVERSED_Z
                    volume.screen.z = max(volume.screen.z, volume.screen.w * 0.00001);
                #else
                    volume.screen.z = min(volume.screen.z, volume.screen.w * 0.99999);
                #endif
                return volume;
            }

            float4 ReadCloud(float3 samplePosition)
            {
                float phase = _Time.y * _Flow;
                float3 curl = sin(samplePosition.yzx * 13.0 + phase + float3(0, 2, 4));
                samplePosition += curl * 0.009 * saturate(1.0 - length(samplePosition) * 1.6);
                // R: density; G/B: integrated sun/sky optical depth; A: color variation.
                return SAMPLE_TEXTURE3D_LOD(_CloudField, sampler_CloudField, samplePosition + 0.5, 0);
            }

            half4 VolumeFragment(VolumeInput volume, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(volume);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(volume);
                float3 rayWS = -GetWorldSpaceNormalizeViewDir(volume.surface);
                float3 eyeWS = GetCameraPositionWS();
                if (unity_OrthoParams.w > 0.5)
                    eyeWS = volume.surface - rayWS * dot(volume.surface - eyeWS, rayWS);
                float3 eye = TransformWorldToObject(eyeWS);
                bool inside = all(abs(eye) < 0.5);
                if (IS_FRONT_VFACE(face, true, false) == inside) discard;

                float3 localRay = TransformWorldToObjectDir(rayWS, false);
                float distanceScale = length(localRay);
                float3 ray = localRay / distanceScale;
                float3 reciprocalRay = rcp(lerp(-1.0, 1.0, step(0.0, ray)) * max(abs(ray), 0.00001));
                float3 a = (-0.5 - eye) * reciprocalRay;
                float3 b = (0.5 - eye) * reciprocalRay;
                float3 nearBounds = min(a, b), farBounds = max(a, b);
                float first = max(0.0, max(nearBounds.x, max(nearBounds.y, nearBounds.z)));
                float last = min(farBounds.x, min(farBounds.y, farBounds.z));
                float2 uv = GetNormalizedScreenSpaceUV(volume.screen);
                float sceneDepth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    sceneDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, sceneDepth);
                #endif
                float3 scenePoint = ComputeWorldSpacePosition(uv, sceneDepth, UNITY_MATRIX_I_VP);
                last = min(last, dot(scenePoint - eyeWS, rayWS) * distanceScale);
                if (first >= last) discard;

                int count = clamp((int)_Samples, 32, 128);
                float stride = (last - first) / count;
                float dither = frac(52.9829189 * frac(dot(volume.screen.xy, float2(0.06711056, 0.00583715))));
                float3 samplePosition = eye + ray * (first + stride * (0.35 + dither * 0.3));
                float transmission = 1.0;
                float3 scattering = 0.0;
                [loop]
                for (int sampleIndex = 0; sampleIndex < count; sampleIndex++)
                {
                    float4 cloud = ReadCloud(samplePosition);
                    if (cloud.r > 0.003)
                    {
                        float sunlight = exp(-cloud.g * _LightAbsorption);
                        float skylight = exp(-cloud.b * 5.0);
                        float3 tint = lerp(_BodyColor.rgb, _VioletColor.rgb, cloud.a * 0.45);
                        float3 litColor = lerp(tint, _PearlColor.rgb, sunlight * sunlight * 0.55);
                        float3 source = _ShadowColor.rgb * (0.35 + skylight * 0.65);
                        source += litColor * (sunlight + _Ambient * skylight);
                        float opacity = 1.0 - exp(-cloud.r * _Extinction * stride);
                        scattering += transmission * opacity * source * _Brightness;
                        transmission *= 1.0 - opacity;
                        if (transmission < 0.008) break;
                    }
                    samplePosition += ray * stride;
                }
                return half4(scattering, 1.0 - transmission);
            }
            ENDHLSL
        }
    }
}
