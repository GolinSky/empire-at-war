Shader "EmpireAtWar/Vfx/Shockwave Refraction"
{
    // A see-through sphere that bends the finished frame behind it. ShockwaveRefractionPass copies the frame after
    // transparents and draws this pass on top, so ships, lasers, fire and the nebula are all refracted.
    Properties
    {
        // How far the shell displaces the scene, as a fraction of the sphere radius.
        _Strength ("Refraction Strength", Range(0, 1)) = 0.3
        // Where the distortion begins: 0 at the sphere's center, 1 at its silhouette.
        _ShellStart ("Shell Start", Range(0, 0.95)) = 0
        // Extra bend of green and blue over red: rainbow fringes on edges seen through the sphere.
        _Chromatic ("Chromatic Split", Range(0, 1)) = 0.3
        // Bands of stronger and weaker bending that travel from the center to the silhouette.
        _RippleCount ("Ripple Count", Range(0, 8)) = 3
        _RippleSpeed ("Ripple Travel (bands per life)", Range(0, 12)) = 5
        _RippleAmount ("Ripple Amount", Range(0, 1)) = 0.6
        // Shimmering heat-haze noise on top of the lens, as a fraction of the sphere's screen radius.
        _Turbulence ("Turbulence", Range(0, 0.2)) = 0.04
        _NoiseScale ("Noise Scale", Range(0.5, 12)) = 4
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1.5
        [HDR] _RimColor ("Rim Color", Color) = (0.45, 0.65, 1, 1)
        _RimIntensity ("Rim Intensity", Range(0, 4)) = 0.5
        // 0..1 age of the wave; set per instance by ShockwaveView.
        _Progress ("Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ShockwaveRefraction"
            // Only ShockwaveRefractionPass draws this tag; URP's forward passes skip it.
            Tags { "LightMode" = "ShockwaveRefraction" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength, _ShellStart, _Chromatic, _RimIntensity, _Progress;
                float _RippleCount, _RippleSpeed, _RippleAmount, _Turbulence, _NoiseScale, _NoiseSpeed;
                half4 _RimColor;
            CBUFFER_END
            TEXTURE2D_X(_ShockwaveSceneColor);
            #include "ShockwaveNoise.hlsl"

            // The low-poly mesh is inflated so its inscribed polygon still covers the exact sphere traced per pixel.
            #define MESH_INFLATE 1.08

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float2 ScreenUv(float3 positionWS)
            {
                float4 positionCS = TransformWorldToHClip(positionWS);
                float2 screen = positionCS.xy * 0.5;
                screen = float2(screen.x, screen.y * _ProjectionParams.x) + positionCS.w * 0.5;
                return screen / positionCS.w;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS * MESH_INFLATE);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 center = UNITY_MATRIX_M._m03_m13_m23;
                // The unit sphere mesh has radius 0.5.
                float radius = length(UNITY_MATRIX_M._m00_m10_m20) * 0.5;
                float3 ray = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 toCenter = center - _WorldSpaceCameraPos;
                float along = dot(toCenter, ray);
                // Distance of the view ray from the center: 0 at the middle, 1 at the silhouette.
                float offAxis = length(toCenter - ray * along) / radius;
                clip(1.0 - offAxis);
                float facing = sqrt(saturate(1.0 - offAxis * offAxis));
                float edge = 1.0 - facing;
                float3 surfaceWS = _WorldSpaceCameraPos + ray * (along - facing * radius);
                float3 normalWS = (surfaceWS - center) / radius;

                float silhouetteFade = 1.0 - smoothstep(0.96, 1.0, edge);
                // The whole disc bends like a lens; the screen offset itself grows towards the silhouette.
                float shell = smoothstep(_ShellStart, 0.9, edge) * silhouetteFade;
                // A Fresnel gradient instead of a thin outline, so the sphere reads as a lit volume.
                float rim = edge * edge * edge * silhouetteFade;
                // Forms within the first 8% of its life, holds, and fades after 55%.
                float envelope = smoothstep(0.0, 0.08, _Progress) * (1.0 - smoothstep(0.55, 1.0, _Progress));

                float2 uv = ScreenUv(surfaceWS);
                // Projecting the surface point pushed outward gives an offset that scales with the on-screen size.
                float2 shiftedUv = ScreenUv(surfaceWS + normalWS * radius * _Strength);
                // Ripple bands start at the center and run out to the silhouette as the wave ages.
                float ripple = sin((edge * _RippleCount - _Progress * _RippleSpeed) * TWO_PI);
                float lens = 1.0 + _RippleAmount * ripple;
                // Noise lives on the sphere surface, so it grows with the wave and keeps churning.
                float3 noisePosition = normalWS * _NoiseScale + _Time.y * _NoiseSpeed;
                float2 jitter = float2(Noise(noisePosition), Noise(noisePosition + 17.3));
                float screenRadius = length(ScreenUv(center + UNITY_MATRIX_V[0].xyz * radius) - ScreenUv(center));
                // Sampling towards the center magnifies the scene like a convex lens.
                float2 offset = ((shiftedUv - uv) * lens + jitter * _Turbulence * screenRadius) * shell * envelope;
                half3 scene;
                scene.r = SAMPLE_TEXTURE2D_X(_ShockwaveSceneColor, sampler_LinearClamp, uv - offset).r;
                scene.g = SAMPLE_TEXTURE2D_X(_ShockwaveSceneColor, sampler_LinearClamp, uv - offset * (1.0 + _Chromatic)).g;
                scene.b = SAMPLE_TEXTURE2D_X(_ShockwaveSceneColor, sampler_LinearClamp, uv - offset * (1.0 + 2.0 * _Chromatic)).b;
                half rimAmount = rim * envelope * _RimIntensity * (0.8 + 0.4 * ripple);
                half3 color = scene + _RimColor.rgb * rimAmount;
                // Full cover wherever the scene is bent, so the unbent frame underneath never shows through.
                return half4(color, saturate(shell * envelope * 4.0 + rimAmount));
            }
            ENDHLSL
        }
    }
}
