Shader "EmpireAtWar/Heavy Laser Beam"
{
    Properties
    {
        [HDR] _CoreColor ("Core", Color) = (2.8, 3.2, 3.5, 1)
        [HDR] _BodyColor ("Body", Color) = (0.08, 0.7, 2.2, 1)
        _BeamAge ("Age", Float) = 0
        _BeamFade ("Remaining Energy", Range(0, 1)) = 1
        _BeamLength ("Length", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor, _BodyColor;
                float _BeamAge, _BeamFade, _BeamLength;
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
                float along = input.uv.x * _BeamLength;
                float flow = along * 1.8 - _BeamAge * 32.0;
                float streak = sin(flow) * sin(flow * 0.43 + 1.7);
                float edge = abs(input.uv.y * 2.0 - 1.0);
                // Disturb only the envelope; the cutting core remains straight.
                float sheathEdge = edge + streak * 0.035;
                float coreWidth = lerp(0.015, 0.13, _BeamFade);
                float aa = max(fwidth(edge), 0.012);
                float core = 1.0 - smoothstep(coreWidth - aa, coreWidth + aa, edge);
                float body = exp2(-edge * edge * 30.0) * (0.9 + streak * 0.1);
                float halo = pow(saturate(1.0 - sheathEdge), 3.0) * 0.22;
                float pulse = pow(saturate(sin(along * 0.7 - _BeamAge * 24.0)), 24.0);
                float flicker = 0.97 + 0.03 * sin(_BeamAge * 113.0);
                // The sheath collapses before the core, which leaves short fading streaks.
                float fragments = lerp(smoothstep(-0.3, 0.3, sin(flow)), 1.0, smoothstep(0.0, 0.6, _BeamFade));
                half3 color = _CoreColor.rgb * core * _BeamFade * fragments;
                color += _BodyColor.rgb * (body * (0.7 + pulse * 0.3) + halo) * _BeamFade * _BeamFade;
                float ignition = smoothstep(0.10, 0.14, _BeamAge);
                return half4(color * flicker * ignition * input.color.a, 0);
            }
            ENDHLSL
        }
    }
}
