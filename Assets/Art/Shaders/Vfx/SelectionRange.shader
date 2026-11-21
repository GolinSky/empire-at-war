Shader "EmpireAtWar/Vfx/SelectionRange"
{
    Properties
    {
        _BaseColor ("Range Color", Color) = (0.40, 0.87, 0.70, 0.85)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "SelectionRange"
            Tags { "LightMode" = "UniversalForward" }

            // Match the capture zones' hull masking when the plane intersects a ship.
            Stencil
            {
                Ref 4
                ReadMask 4
                WriteMask 0
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionXZ : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionXZ = input.positionOS.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.positionXZ);
                float pixelWidth = max(fwidth(radius), 0.001);
                half fade = smoothstep(0.79, 1.0, radius);
                half edge = 1.0 - smoothstep(1.0 - pixelWidth, 1.0, radius);
                half rim = smoothstep(0.986 - pixelWidth, 0.986, radius);
                half alpha = edge * max(fade * 0.26, rim * 0.67);
                return half4(_BaseColor.rgb, _BaseColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
