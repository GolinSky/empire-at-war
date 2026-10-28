Shader "EmpireAtWar/Vfx/Map Space Dust"
{
    Properties
    {
        _MainTex ("Dust (R), Wisp (G)", 2D) = "white" {}
        _DustColor ("Dust Tint", Color) = (0.62, 0.72, 0.82, 0.55)
        _WispColor ("Wisp Tint", Color) = (0.34, 0.43, 0.52, 0.04)
        _PocketColor ("Dense Fog Pocket Tint", Color) = (0.38, 0.46, 0.55, 0.24)
        _FieldSpan ("Local Field Width", Float) = 3200
        _MapBounds ("Map XZ Bounds", Vector) = (-4500, -4500, 4500, 4500)
        _HeightRange ("Altitude Range", Vector) = (-650, 180, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-50" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _DustColor;
                half4 _WispColor;
                half4 _PocketColor;
                float _FieldSpan;
                float4 _MapBounds;
                float4 _HeightRange;
            CBUFFER_END

            // Each quad stores one normalized world seed; UV1 stores size, kind, rotation and pocket weight.
            struct Attributes { float3 seed : POSITION; float2 uv : TEXCOORD0; float4 data : TEXCOORD1; half4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float eyeDepth : TEXCOORD1; float kind : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 seed = input.seed.xz * _FieldSpan + _Time.y * float2(0.22, -0.13);
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                float middleHeight = (_HeightRange.x + _HeightRange.y) * 0.5;
                float focusDistance = abs(forward.y) > 0.1
                    ? max(0, (middleHeight - _WorldSpaceCameraPos.y) / forward.y) : 0;
                float2 fieldCenter = _WorldSpaceCameraPos.xz + forward.xz * focusDistance;
                // Wrapping changes only distant, fully faded cards. Nearby dust stays anchored while panning.
                float2 offset = (frac((seed - fieldCenter) / _FieldSpan + 0.5) - 0.5) * _FieldSpan;
                float3 center = float3(fieldCenter.x + offset.x,
                    lerp(_HeightRange.x, _HeightRange.y, input.seed.y), fieldCenter.y + offset.y);
                float edgeFade = 1 - smoothstep(_FieldSpan * 0.35, _FieldSpan * 0.48, max(abs(offset.x), abs(offset.y)));
                float2 mapEdge = min(center.xz - _MapBounds.xy, _MapBounds.zw - center.xz);
                edgeFade *= saturate(min(mapEdge.x, mapEdge.y) / 100);
                float cameraFade = smoothstep(lerp(12, 100, input.data.y), lerp(80, 350, input.data.y),
                    distance(center, _WorldSpaceCameraPos));
                float s, c;
                sincos(input.data.z, s, c);
                float2 corner = input.uv * 2 - 1;
                corner.y *= lerp(1, 0.3, input.data.y);
                corner = float2(c * corner.x - s * corner.y, s * corner.x + c * corner.y) * input.data.x;
                corner *= step(0.0001, edgeFade * cameraFade);
                float3 positionWS = center + UNITY_MATRIX_V[0].xyz * corner.x + UNITY_MATRIX_V[1].xyz * corner.y;
                output.position = TransformWorldToHClip(positionWS);
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                output.uv = input.uv;
                output.kind = input.data.y;
                half4 fogColor = lerp(_WispColor, _PocketColor, input.data.w);
                output.color = lerp(_DustColor, fogColor, input.data.y) * input.color;
                output.color.a *= edgeFade * cameraFade;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half2 masks = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rg;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(input.position)), _ZBufferParams);
                // Fade through hulls instead of drawing hard intersections as ships cross a wisp.
                half softFade = saturate((sceneDepth - input.eyeDepth) / lerp(2, 35, input.kind));
                return half4(input.color.rgb, input.color.a * lerp(masks.r, masks.g, input.kind) * softFade);
            }
            ENDHLSL
        }
    }
}
