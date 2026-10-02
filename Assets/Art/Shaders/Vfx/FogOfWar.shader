Shader "Custom/URP_FogOfWar"
{
    Properties
    {
        _MainTex ("Fog Mask Texture", 2D) = "black" {} // Black means no visibility
        _Color ("Cell Color", Color) = (1, 1, 1, 0.15)
        
        _GridSize ("Grid Cell Size", Float) = 2.0
        _GridThickness ("Cell Gap (Fraction)", Range(0, 0.5)) = 0.12
    }
    SubShader
    {
        // Transparent queue, ZWrite off because it's an overlay
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
                float4 _Color;
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
                // Read visibility from mask texture
                // r channel: 1 means fully visible (no fog), 0 means full fog
                half visibility = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;

                // Fill cell interiors, leaving transparent gaps on the world-space XZ grid.
                float2 gridPosition = i.positionWS.xz / _GridSize;
                float2 gridUv = frac(gridPosition);
                float2 edgeDistance = min(gridUv, 1.0 - gridUv);
                float halfGap = _GridThickness * 0.5;
                float2 antialiasWidth = max(fwidth(gridPosition), 0.0001);
                float2 cellCoverage = smoothstep(halfGap, halfGap + antialiasWidth, edgeDistance);
                half cellMask = cellCoverage.x * cellCoverage.y;
                
                // Calculate final alpha based on visibility. 
                // If visibility is 1 (revealed), alpha turns to 0 making it fully transparent.
                half finalAlpha = _Color.a * cellMask * (1.0 - visibility);
                
                return half4(_Color.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
