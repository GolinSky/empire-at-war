Shader "EmpireAtWar/ReinforcementZone"
{
    Properties
    {
        _BaseColor ("Zone Color", Color) = (0.48, 0.55, 0.62, 0.08)
        _BorderOpacity ("Border Opacity", Range(0, 1)) = 0.48
        _DepthFadeDistance ("Intersection Fade Distance", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Zone"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _BorderOpacity;
                float _DepthFadeDistance;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.eyeDepth = -position.positionVS.z;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float antialias = max(fwidth(radius), 0.001);
                float edgeDistance = abs(radius - 0.95);
                half border = 1.0 - smoothstep(0.009, 0.009 + antialias, edgeDistance);
                half outline = 1.0 - smoothstep(0.032, 0.032 + antialias, edgeDistance);
                half fill = 1.0 - smoothstep(0.91, 0.95, radius);

                // The dark keyline separates the muted color from similar backgrounds.
                half fillAlpha = fill * _BaseColor.a * 0.3;
                half outlineAlpha = outline * 0.32;
                half borderAlpha = border * _BorderOpacity;
                half3 color = lerp(_BaseColor.rgb, half3(0.025, 0.04, 0.06), outline);
                color = lerp(color, _BaseColor.rgb, border);

                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                if (unity_OrthoParams.w > 0.5)
                {
                    sceneDepth = LinearDepthToEyeDepth(rawDepth);
                }

                // Keep the tactical outline complete over nebulae, hulls and placement previews.
                // Only the interior fades against opaque geometry.
                fillAlpha *= saturate((sceneDepth - input.eyeDepth) / _DepthFadeDistance);
                half alpha = max(fillAlpha, max(outlineAlpha, borderAlpha));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
