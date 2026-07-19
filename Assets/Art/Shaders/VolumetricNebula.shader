// File: VolumetricNebula.shader
Shader "Custom/VolumetricNebula"
{
    Properties
    {
        _MainTex ("Main Noise", 2D) = "white" {}
        _DetailTex ("Detail Noise", 2D) = "white" {}
        _Color ("Nebula Color", Color) = (1, 0.5, 1, 1)
        _Intensity ("Intensity", Float) = 1.0
        _Falloff ("Falloff Strength", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _DetailTex;
            float4 _MainTex_ST;
            float4 _DetailTex_ST;
            float4 _Color;
            float _Intensity;
            float _Falloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5; // center the UVs
                float dist = length(uv); // distance from center

                float falloff = exp(-_Falloff * dist * dist); // smoother dark edges

                float mainNoise = tex2D(_MainTex, i.uv).r;
                float detailNoise = tex2D(_DetailTex, i.uv * 4.0).r;

                float alpha = mainNoise * detailNoise * falloff * _Intensity;

                return float4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
