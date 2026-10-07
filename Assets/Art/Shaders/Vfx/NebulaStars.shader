Shader "EmpireAtWar/Vfx/Nebula Stars"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-450" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 position : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position);
                output.uv = input.uv * 2 - 1;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = abs(input.uv);
                float core = exp(-dot(p,p) * 34);
                float halo = exp(-dot(p,p) * 6) * 0.055;
                float flare = (exp(-p.x * 90 - p.y * 5) + exp(-p.y * 90 - p.x * 5)) * 0.12;
                float edge = saturate((1 - max(p.x,p.y)) * 8);
                return half4(input.color.rgb * input.color.a * (core + halo + flare) * edge * 3, 0);
            }
            ENDHLSL
        }
    }
}
