// ShipLit.shader
// -----------------------------------------------------------------------------
// One lit shader for every ship, station and platform, with per-owner team colors.
//
// Design goals
//   * One compiled variant for all unit materials: no per-material keywords, every map
//     is always sampled with neutral defaults. Materials that share a shader variant
//     are grouped by the SRP Batcher, which is what makes many ships cheap.
//   * Team color without extra materials: the owner's palette index arrives through
//     MeshRenderer.SetShaderUserValue (see TeamColorView.cs), which keeps both the
//     SRP Batcher and the GPU Resident Drawer working.
//   * "Simplified lit": URP's physically based lighting, but only the features ships
//     use - metallic workflow, normal, occlusion, emission. No parallax, detail maps,
//     clear coat, transparency, alpha clipping or lightmaps.
//
// Property names match URP/Lit (_BaseMap, _BumpMap, _MetallicGlossMap, ...) so switching
// an existing material to this shader keeps its textures.
// -----------------------------------------------------------------------------
Shader "EmpireAtWar/Ship Lit"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [NoScaleOffset][Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Float) = 1.0
        [NoScaleOffset] _OcclusionMap("Occlusion (G)", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Team Color)]
        [NoScaleOffset] _TeamMaskMap("Team Mask (R)", 2D) = "black" {}
        _TeamMaskStrength("Mask Strength", Range(0.0, 1.0)) = 1.0
        _TeamRimStrength("Rim Strength", Range(0.0, 4.0)) = 0.6
        _TeamRimPower("Rim Sharpness", Range(0.5, 8.0)) = 3.0
        _TeamEmissionTint("Emission Tint", Range(0.0, 1.0)) = 0.5

        // URP's shared shadow/depth passes read _Cutoff; ships never clip, so it stays hidden.
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ---------------------------------------------------------------------
        // Color pass: lighting, shadows received, fog, team color.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShipLitPassVertex
            #pragma fragment ShipLitPassFragment

            // Pipeline keywords: URP turns these on and off from the quality settings and
            // the scene (shadows, light count, SSAO, Forward+). multi_compile compiles a
            // variant for each combination; the "_" entry means "keyword off".
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            // Instancing. "renderinglayer" makes Unity send each instance's rendering-layer
            // data, which is where the renderer user value (our team index) is stored.
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ShipLitInput.hlsl"
            #include "ShipLitForwardPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Shadow map pass: ships cast shadows. Standard URP code, reused as-is because
        // it only needs the declarations from ShipLitInput.hlsl.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // Point/spot lights need the light position instead of a direction.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "ShipLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Depth prepass: fills the depth buffer early when URP asks for it.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ShipLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Depth + normals: used by SSAO.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShipLitDepthNormalsVertex
            #pragma fragment ShipLitDepthNormalsFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ShipLitInput.hlsl"
            #include "ShipLitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    // If this shader cannot run, Unity draws the error shader instead of silently falling back.
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
