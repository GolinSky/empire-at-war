// EngineGlow.shader
// -----------------------------------------------------------------------------
// Additive, unlit engine glow that fades between thrust, idle and off on the GPU.
//
//   * The fade runs in the vertex shader from _Time: the CPU writes one value per
//     glow renderer only when the ship's engine state changes (ShipEngineGlowComponent).
//   * That value arrives through MeshRenderer.SetShaderUserValue, like the team color in
//     ShipLitInput.hlsl, so the SRP Batcher and the GPU Resident Drawer keep working.
//     No MaterialPropertyBlock, no material instance.
//   * Encoding (EngineGlowUserValue.cs must match):
//       bits 0-7   : 255 - level at fade start * 255   (0 -> fade started at full thrust)
//       bits 8-9   : target state, 0 thrust / 1 idle / 2 off
//       bits 10-31 : fade start in 10 ms ticks of _Time.y, modulo 2^22
//     User value 0 is "settled at full thrust": renderers nobody drives draw at full brightness.
// -----------------------------------------------------------------------------
Shader "EmpireAtWar/Engine Glow"
{
    Properties
    {
        [MainTexture] _BaseMap("Glow", 2D) = "white" {}
        [MainColor][HDR] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _IdleIntensity("Idle Intensity", Range(0.0, 1.0)) = 0.35
        _FadeSeconds("Fade Seconds", Range(0.05, 5.0)) = 0.6
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "EngineGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EngineGlowVertex
            #pragma fragment EngineGlowFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ENGINE_GLOW_FROM_MASK 0xFFu
            #define ENGINE_GLOW_STATE_SHIFT 8
            #define ENGINE_GLOW_STATE_MASK 0x3u
            #define ENGINE_GLOW_TIME_SHIFT 10
            #define ENGINE_GLOW_TIME_MASK 0x3FFFFFu
            #define ENGINE_GLOW_TICKS_PER_SECOND 100.0
            #define ENGINE_GLOW_STATE_THRUST 0u
            #define ENGINE_GLOW_STATE_IDLE 1u

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _IdleIntensity;
                half _FadeSeconds;
                half _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half level : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Same formula as EngineGlowUserValue.Evaluate.
            half EvaluateEngineGlowLevel(uint userValue)
            {
                half fromLevel = 1.0h - (half)(userValue & ENGINE_GLOW_FROM_MASK) / 255.0h;
                uint state = (userValue >> ENGINE_GLOW_STATE_SHIFT) & ENGINE_GLOW_STATE_MASK;
                half targetLevel = state == ENGINE_GLOW_STATE_THRUST ? 1.0h
                    : state == ENGINE_GLOW_STATE_IDLE ? _IdleIntensity : 0.0h;
                uint startTicks = userValue >> ENGINE_GLOW_TIME_SHIFT;
                uint nowTicks = (uint)(_Time.y * ENGINE_GLOW_TICKS_PER_SECOND) & ENGINE_GLOW_TIME_MASK;
                uint elapsedTicks = (nowTicks - startTicks) & ENGINE_GLOW_TIME_MASK;
                half progress = saturate(elapsedTicks / (ENGINE_GLOW_TICKS_PER_SECOND * max(_FadeSeconds, 0.001h)));
                return lerp(fromLevel, targetLevel, progress);
            }

            Varyings EngineGlowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.level = EvaluateEngineGlowLevel(unity_RendererUserValue);
                return output;
            }

            half4 EngineGlowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                color.a *= input.level;
                return color;
            }
            ENDHLSL
        }
    }
}
