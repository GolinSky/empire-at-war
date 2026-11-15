Shader "EmpireAtWar/Vfx/Turbolaser Bolt"
{
    Properties
    {
        _BoltMask ("Bolt Mask", 2D) = "black" {}
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ColorMask RGB
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BoltMask);
            SAMPLER(sampler_BoltMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_BoltMask, sampler_BoltMask, input.uv).r;
                half core = smoothstep(0.45h, 0.9h, mask);
                half3 color = lerp(input.color.rgb, _CoreColor.rgb, core) * mask * 2.7h;
                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                float sceneDepth = IsPerspectiveProjection()
                    ? LinearEyeDepth(rawDepth, _ZBufferParams)
                    : LinearDepthToEyeDepth(rawDepth);
                float boltDepth = -TransformWorldToView(input.positionWS).z;
                half fade = saturate((sceneDepth - boltDepth) * 0.5) * saturate(boltDepth - 1.0);
                half fog = InitializeInputDataFog(float4(input.positionWS, 1), input.fogFactor);
                return half4(MixFogColor(color, half3(0, 0, 0), fog), input.color.a * fade);
            }
            ENDHLSL
        }
    }
}
