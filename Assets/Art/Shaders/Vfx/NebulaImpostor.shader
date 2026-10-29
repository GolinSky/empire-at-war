Shader "EmpireAtWar/Vfx/Nebula Impostor"
{
    Properties { _MainTex ("Baked Nebula", 2D) = "black" {} }
    SubShader
    {
        // Draw after the skybox/stars, with depth testing so foreground geometry stays unobscured.
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-440" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv); }
            ENDHLSL
        }
    }
}
