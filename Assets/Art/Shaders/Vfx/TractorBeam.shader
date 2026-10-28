Shader "EmpireAtWar/Tractor Beam"
{
    Properties
    {
        [HDR] _CoreColor ("Core", Color) = (1.4, 1.5, 2.0, 1)
        [HDR] _BodyColor ("Energy field", Color) = (0.25, 0.16, 1.0, 1)
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
                float along = input.uv.x;
                // Taper in the shader to avoid skewed UVs across a two-point line's triangles.
                float edge = abs(input.uv.y * 2.0 - 1.0) / lerp(0.061, 1.0, along);
                float envelope = pow(saturate(1.0 - edge * edge), 3.0);
                float bands = pow(0.5 + 0.5 * sin(along * 90.0 + _Time.y * 7.0), 10.0);
                float wisps = 0.85 + 0.15 * sin(along * 41.0 - _Time.y * 2.0 + edge * 13.0);
                // Keep the core narrow as the surrounding field expands toward the target.
                float core = exp2(-edge * edge * lerp(120.0, 600.0, along));
                float ends = smoothstep(0.0, 0.025, along) * (1.0 - smoothstep(0.94, 1.0, along));
                float contact = exp2(-pow((along - 0.96) * 35.0, 2.0) - edge * edge * 12.0);
                half3 color = _BodyColor.rgb * envelope * (0.24 * wisps + 0.18 * bands);
                color += _CoreColor.rgb * (core * 0.65 + contact * 0.18);
                return half4(color * ends * input.color.a, 0);
            }
            ENDHLSL
        }
    }
}
