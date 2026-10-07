Shader "EmpireAtWar/Vfx/Nebula Filament Volume"
{
    Properties
    {
        _CloudField ("Emission and Dust (3D)", 3D) = "black" {}
        _DetailTex ("Fine Turbulence (3D)", 3D) = "gray" {}
        [HDR] _CoolColor ("Ionized Gas", Color) = (0.12,0.45,0.8,1)
        [HDR] _WarmColor ("Dust Rims", Color) = (0.65,0.24,0.08,1)
        _DustColor ("Dust Interior", Color) = (0.009,0.006,0.015,1)
        _Emission ("Gas Emission", Range(0,10)) = 3
        _Extinction ("Dust Extinction", Range(0,40)) = 18
        _Samples ("Ray Samples", Range(48,160)) = 112
        _Flow ("Internal Drift", Range(0,0.02)) = 0.002
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-440" "DisableBatching"="True" }
        Pass
        {
            Name "FilamentVolume"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE3D(_CloudField); SAMPLER(sampler_CloudField);
            TEXTURE3D(_DetailTex); SAMPLER(sampler_DetailTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _CoolColor, _WarmColor, _DustColor;
                float _Emission, _Extinction, _Samples, _Flow;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 eyeOS : TEXCOORD0;
                float3 rayOS : TEXCOORD1;
                float rayEyeDepth : TEXCOORD2;
            };

            Varyings Vert(float3 vertex : POSITION)
            {
                Varyings output;
                float3 surface = TransformObjectToWorld(vertex);
                float3 eye = GetCameraPositionWS();
                float3 ray = -GetWorldSpaceViewDir(surface);
                if (unity_OrthoParams.w > 0.5)
                    eye = surface - ray * dot(surface - eye, ray);
                output.eyeOS = TransformWorldToObject(eye);
                output.rayOS = TransformWorldToObjectDir(ray, false);
                output.rayEyeDepth = dot(ray, GetViewForwardDir());
                output.positionCS = TransformWorldToHClip(surface);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float localLength = length(input.rayOS);
                float3 ray = input.rayOS / localLength;
                float3 inverseRay = rcp(lerp(-1.0, 1.0, step(0.0, ray)) * max(abs(ray), 0.00001));
                float3 a = (-0.5 - input.eyeOS) * inverseRay;
                float3 b = (0.5 - input.eyeOS) * inverseRay;
                float3 nearBounds = min(a,b), farBounds = max(a,b);
                float first = max(0, max(nearBounds.x, max(nearBounds.y, nearBounds.z)));
                float last = min(farBounds.x, min(farBounds.y, farBounds.z));
                float depth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                float sceneDepth = unity_OrthoParams.w > 0.5 ? LinearDepthToEyeDepth(depth) : LinearEyeDepth(depth, _ZBufferParams);
                last = min(last, sceneDepth * localLength / input.rayEyeDepth);
                if (first >= last) discard;

                int count = clamp((int)_Samples,48,160);
                float stride = (last - first) / count;
                float jitter = frac(52.9829189 * frac(dot(input.positionCS.xy,float2(0.06711056,0.00583715))));
                float3 p = input.eyeOS + ray * (first + stride * (0.25 + jitter * 0.5));
                float transmission = 1;
                float3 radiance = 0;
                [loop] for (int i = 0; i < count; i++)
                {
                    float detail = SAMPLE_TEXTURE3D_LOD(_DetailTex,sampler_DetailTex,p * 9 + _Time.y * _Flow * float3(0.2,0.13,0.07),0).r;
                    float3 warp = sin(p.yzx * 17 + _Time.y * _Flow) * 0.002;
                    float4 field = SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,p + 0.5 + warp,0);
                    float erosion = saturate(detail * 3.2 - 0.7);
                    float extinction = (field.b * _Extinction + field.a * 1.5) * erosion;
                    float opacity = 1 - exp(-extinction * stride);
                    float3 emission = (field.r * _CoolColor.rgb + field.g * _WarmColor.rgb) * _Emission * erosion;
                    radiance += transmission * (emission * stride + _DustColor.rgb * opacity);
                    transmission *= 1 - opacity;
                    if (transmission < 0.005) break;
                    p += ray * stride;
                }
                return half4(radiance, 1 - transmission);
            }
            ENDHLSL
        }
    }
}
