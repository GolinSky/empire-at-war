Shader "EmpireAtWar/CaptureZoneGrid"
{
    Properties
    {
        _BaseColor ("Grid Color", Color) = (0.38, 0.67, 0.89, 0.32)
        [Toggle(_SQUARE_GRID)] _SquareGrid ("Square Grid", Float) = 0
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
            Name "ZoneGrid"
            Tags { "LightMode" = "UniversalForward" }

            // Ship Lit marks hull pixels even where the zone plane passes through the ship.
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
            #pragma shader_feature_local_fragment _SQUARE_GRID
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half LineCoverage(float distance, float halfWidth, float pixelWidth)
            {
                float antialias = max(pixelWidth * 0.5, 0.001);
                return (1.0 - smoothstep(max(halfWidth - antialias, 0.0), halfWidth + antialias, distance))
                    * saturate(halfWidth / antialias);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 position = input.uv * 2.0 - 1.0;
                float radius = length(position);
                float2 pixel = fwidth(position);

                #if defined(_SQUARE_GRID)
                const float CELL_SPACING = 0.018;
                const float CORNER_RADIUS = 0.13;
                float2 cell = abs(frac(position / CELL_SPACING) - 0.5);
                float2 corner = cell - (0.47 - CORNER_RADIUS);
                float distance = length(max(corner, 0.0)) + min(max(corner.x, corner.y), 0.0) - CORNER_RADIUS;
                float pixelWidth = max(pixel.x, pixel.y) / CELL_SPACING;
                half cells = LineCoverage(abs(distance), 0.045, pixelWidth);
                // Fade unresolved cells at distant zoom levels to avoid shimmer.
                half detail = 1.0 - smoothstep(0.3, 0.85, pixelWidth);
                float edgeWidth = max(fwidth(radius), 0.001);
                half circle = 1.0 - smoothstep(1.0, 1.0 + edgeWidth, radius);
                // Keep mid-radius lines faint while fading continuously from the rim to the center.
                float radialProgress = saturate(radius);
                half outwardFade = 0.5 * radialProgress * radialProgress * radialProgress;
                half grid = (0.012 + cells * detail * 0.988) * outwardFade * circle;

                float angle = atan2(position.y, position.x + 0.000001);
                float dash = abs(frac(angle * (24.0 / TWO_PI) + 0.5) - 0.5);
                half rim = LineCoverage(abs(radius - 0.99), 0.0035, edgeWidth);
                rim *= 0.5 * (1.0 - smoothstep(0.33, 0.43, dash));
                half alpha = max(grid, rim * circle);
                #else
                float pixelWidth = max(pixel.x, pixel.y);

                // Four rings and twelve radial spokes, with a clear center.
                float ringIndex = clamp(round(radius / 0.22), 1.0, 4.0);
                float ringDistance = abs(radius - ringIndex * 0.22);
                half rings = LineCoverage(ringDistance, 0.008, pixelWidth);
                rings *= lerp(0.69, 1.0, 1.0 - fmod(ringIndex, 2.0));

                float angle = atan2(position.y, position.x + 0.000001);
                float sector = angle * (12.0 / TWO_PI);
                float spokeDistance = abs(sector - round(sector)) * (TWO_PI / 12.0) * radius;
                half spokes = LineCoverage(spokeDistance, 0.006, pixelWidth);
                spokes *= 0.6 * smoothstep(0.11, 0.14, radius);

                // Taper the grid across its outer half and leave a faint dashed rim.
                half fade = 1.0 - smoothstep(0.5, 1.0, radius);
                float dash = abs(frac(sector * 2.0 + 0.5) - 0.5);
                half rim = LineCoverage(abs(radius - 0.96), 0.006, pixelWidth);
                rim *= 0.3 * (1.0 - smoothstep(0.33, 0.43, dash));
                half alpha = max((0.055 + max(rings, spokes)) * fade, rim);
                #endif
                return half4(_BaseColor.rgb, _BaseColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
