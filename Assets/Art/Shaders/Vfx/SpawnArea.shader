Shader "Custom/URP_SpawnArea"
{
    Properties
    {
        _MainTex ("Spawn Mask Texture", 2D) = "black" {} // r: 0 hidden, 0.5 blocked, 1 open
        _OpenColor ("Open Cell Color", Color) = (0.3, 1, 0.45, 0.03)
        _BlockedColor ("Blocked Cell Color", Color) = (1, 0.3, 0.25, 0.04)

        _GridSize ("Grid Cell Size", Float) = 11.0
        _GridThickness ("Cell Gap (Fraction)", Range(0, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OpenColor;
                float4 _BlockedColor;
                float _GridSize;
                float _GridThickness;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half state = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                half open = saturate((state - 0.5) * 2.0);
                half blocked = saturate(state * 2.0) - open;

                float2 gridPosition = i.positionWS.xz / _GridSize;
                float2 gridUv = frac(gridPosition);
                float2 edgeDistance = min(gridUv, 1.0 - gridUv);
                float halfGap = _GridThickness * 0.5;
                float2 antialiasWidth = max(fwidth(gridPosition), 0.0001);
                float2 cellCoverage = smoothstep(halfGap, halfGap + antialiasWidth, edgeDistance);
                half cellMask = cellCoverage.x * cellCoverage.y;

                half alpha = (_OpenColor.a * open + _BlockedColor.a * blocked) * cellMask;
                half3 color = (_OpenColor.rgb * open + _BlockedColor.rgb * blocked) / max(open + blocked, 0.0001);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
