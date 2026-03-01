Shader "Custom/URP_FogOfWar"
{
    Properties
    {
        _MainTex ("Fog Mask Texture", 2D) = "black" {} // Black means no visibility
        _Color ("Fog Color", Color) = (0.05, 0.05, 0.05, 0.95)
        
        _GridColor ("Grid Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _GridSize ("Grid Cell Size", Float) = 2.0
        _GridThickness ("Grid Thickness", Float) = 0.05
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
                float4 _GridColor;
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

                // Create the grid pattern using world space coordinates (xz plane)
                float2 gridUv = frac(i.positionWS.xz / _GridSize);
                
                // Calculate grid lines (smooth lines for better look)
                float2 df = fwidth(i.positionWS.xz / _GridSize);
                float lineThickness = _GridThickness;
                float2 gridLines = smoothstep(lineThickness + df, lineThickness, gridUv) 
                                 + smoothstep(1.0 - lineThickness - df, 1.0 - lineThickness, gridUv);
                
                half gridIntensity = saturate(gridLines.x + gridLines.y);

                // Blend fog base color with the grid lines
                half4 fogColor = lerp(_Color, _GridColor, gridIntensity * _GridColor.a);
                
                // Calculate final alpha based on visibility. 
                // If visibility is 1 (revealed), alpha turns to 0 making it fully transparent.
                half finalAlpha = fogColor.a * (1.0 - visibility);
                
                return half4(fogColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
