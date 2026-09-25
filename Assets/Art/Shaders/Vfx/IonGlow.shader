Shader "EmpireAtWar/Ion Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.12, 0.65, 1, 1)
        _Brightness ("Brightness", Float) = 3
        _Roundness ("Round Pulse", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Brightness, _Roundness;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(float2(p.x * _Roundness, p.y));
                float glow = pow(saturate(1.0 - radius), 2.0);
                float core = pow(saturate(1.0 - radius * 2.5), 3.0);
                half3 color = lerp(_Color.rgb * input.color.rgb, half3(0.8, 0.95, 1.0), core);
                return half4(color * _Brightness, glow * input.color.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
