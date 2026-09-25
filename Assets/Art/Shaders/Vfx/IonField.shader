Shader "EmpireAtWar/Ion Field"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.08, 0.5, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Opacity;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionOS = input.positionOS;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 p = input.positionOS;
                float time = _Time.y;
                float warp = sin(p.z * 19.0 + time * 5.0) * 0.7 + sin(p.x * 31.0 - time * 3.0) * 0.35;
                float bands = pow(saturate(1.0 - abs(sin(p.y * 17.0 + p.z * 7.0 + warp + time * 2.0))), 18.0);
                float flicker = 0.65 + 0.35 * sin(time * 23.0 + p.z * 21.0);
                float rim = pow(1.0 - saturate(dot(normalize(input.normalWS),
                    GetWorldSpaceNormalizeViewDir(input.positionWS))), 3.0);
                float alpha = (rim * 0.17 + bands * 0.28) * flicker * _Opacity;
                return half4(_Color.rgb * 2.0, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
