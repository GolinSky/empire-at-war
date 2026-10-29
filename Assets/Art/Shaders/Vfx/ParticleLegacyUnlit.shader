Shader "EmpireAtWar/Vfx/Particle Legacy Unlit"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ColorMask RGB
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile _ SOFTPARTICLES_ON
            #pragma shader_feature_local_fragment _ _LEGACY_ADDITIVE _LEGACY_PREMULTIPLY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                float _InvFade;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float fogFactor : TEXCOORD1; float eyeDepth : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                output.eyeDepth = -position.positionVS.z;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                #if defined(SOFTPARTICLES_ON)
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(input.positionCS.xy / _ScaledScreenParams.xy), _ZBufferParams);
                    input.color.a *= saturate(_InvFade * (sceneDepth - input.eyeDepth));
                #endif
                half4 color = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                #if defined(_LEGACY_PREMULTIPLY)
                    return color * input.color.a;
                #else
                    color *= 2.0h * _TintColor;
                    color.a = saturate(color.a);
                    #if defined(_LEGACY_ADDITIVE)
                        color.rgb = MixFogColor(color.rgb, half3(0, 0, 0), input.fogFactor);
                    #else
                        color.rgb = MixFog(color.rgb, input.fogFactor);
                    #endif
                    return color;
                #endif
            }
            ENDHLSL
        }
    }
}
