Shader "EmpireAtWar/ConstructionReveal"
{
    Properties
    {
        [HDR] _Color ("Hologram Color", Color) = (0.1, 1, 0.3, 0.55)
        [HDR] _EdgeColor ("Build Edge Color", Color) = (0.6, 3, 1, 1)
        _BuildProgress ("Build Progress", Range(0, 1)) = 0
        _BuildMinY ("World Bottom", Float) = 0
        _BuildMaxY ("World Top", Float) = 1
        _EdgeWidth ("Edge Height Fraction", Range(0.001, 0.1)) = 0.025
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                float _BuildProgress;
                float _BuildMinY;
                float _BuildMaxY;
                float _EdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float height = (input.positionWS.y - _BuildMinY) / max(_BuildMaxY - _BuildMinY, 0.001);
                clip(_BuildProgress - height);
                clip(_BuildProgress - 0.00001);
                half edge = 1.0h - smoothstep(0.0, _EdgeWidth, _BuildProgress - height);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0h - saturate(dot(normalize(input.normalWS), viewDirection)), 2.0h);
                half scan = 0.75h + 0.25h * sin(input.positionWS.y * 3.0 - _Time.y * 2.0);
                half3 color = lerp(_Color.rgb * (0.5h + rim) * scan, _EdgeColor.rgb, edge);
                half alpha = lerp(_Color.a * (0.45h + 0.55h * rim), _EdgeColor.a, edge);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
