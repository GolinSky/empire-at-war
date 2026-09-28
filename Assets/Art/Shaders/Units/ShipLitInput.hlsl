// ShipLitInput.hlsl
// -----------------------------------------------------------------------------
// Everything every pass of "EmpireAtWar/Ship Lit" needs to know about a material:
// textures, the per-material constant buffer, and the team-color helpers.
//
// Why one shared file?
//   The SRP Batcher can only batch a shader when EVERY pass declares the exact same
//   UnityPerMaterial constant buffer (same variables, same order). Keeping it in one
//   include makes that impossible to get wrong.
// -----------------------------------------------------------------------------
#ifndef SHIP_LIT_INPUT_INCLUDED
#define SHIP_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// SurfaceInput.hlsl declares _BaseMap, _BumpMap and _EmissionMap (texture + sampler)
// plus small helpers such as Alpha() and SampleAlbedoAlpha(). URP's own shadow and
// depth passes call those helpers, so we include it instead of redeclaring them.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// -----------------------------------------------------------------------------
// Extra textures
// -----------------------------------------------------------------------------
// TEXTURE2D only declares the texture. We deliberately sample all of them with
// sampler_BaseMap (declared in SurfaceInput.hlsl): GPUs have a small sampler budget,
// and every ship texture uses the same filtering/wrap settings anyway.
TEXTURE2D(_MetallicGlossMap);   // R = metallic, A = smoothness (same packing as URP Lit)
TEXTURE2D(_OcclusionMap);       // G = ambient occlusion (same channel URP Lit reads)
TEXTURE2D(_TeamMaskMap);        // R = where the team color is painted (0 = keep albedo)

// -----------------------------------------------------------------------------
// Per-material constants
// -----------------------------------------------------------------------------
// Every property from the Properties block that is NOT a texture must live here.
// Texture tiling/offset (_BaseMap_ST) counts as a property too.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;               // Unused by ships, but URP's shared passes reference it.
    half _BumpScale;
    half _Metallic;
    half _Smoothness;
    half _OcclusionStrength;
    half4 _EmissionColor;
    half _TeamMaskStrength;
    half _TeamRimStrength;
    half _TeamRimPower;
    half _TeamEmissionTint;
CBUFFER_END

// -----------------------------------------------------------------------------
// Team colors
// -----------------------------------------------------------------------------
// The palette is a GLOBAL array uploaded once per match from C# (TeamColorPalette).
// It is not inside UnityPerMaterial, so all ships share it and nothing about it
// breaks batching.
#define MAX_TEAM_COLORS 8
float4 _TeamColors[MAX_TEAM_COLORS];

// Which palette entry a renderer uses comes from MeshRenderer.SetShaderUserValue(uint).
// Unity stores that number with the renderer's built-in per-draw data (not in the
// material), so one material can be shared by every player's ships and still be
// colored per ship - without MaterialPropertyBlocks, which would break the SRP Batcher.
//
// Encoding (see TeamColorView.cs):
//   0      -> the renderer has no owner (prefab preview, editor): no team color.
//   n > 0  -> palette index n - 1.
#define NO_TEAM_USER_VALUE 0u

// Returns rgb = team color, a = 1 when the renderer has a team, 0 otherwise.
// Using "a" as an on/off weight lets callers blend with lerp() instead of branching.
half4 GetTeamColor()
{
    uint userValue = unity_RendererUserValue;
    if (userValue == NO_TEAM_USER_VALUE)
    {
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    // min() protects against an out-of-range index reading garbage memory.
    uint paletteIndex = min(userValue - 1u, (uint)(MAX_TEAM_COLORS - 1));
    return half4(_TeamColors[paletteIndex].rgb, 1.0h);
}

// Recolors the painted parts of the hull. We keep the albedo's brightness and swap
// its hue for the team color, so panel detail and dirt in the texture survive.
half3 ApplyTeamMask(half3 albedo, half teamMask, half4 teamColor)
{
    half luminance = dot(albedo, half3(0.299h, 0.587h, 0.114h));
    half3 tinted = luminance * teamColor.rgb * 2.0h;   // x2 keeps mid-grey paint at full color
    half weight = saturate(teamMask * _TeamMaskStrength) * teamColor.a;
    return lerp(albedo, tinted, weight);
}

// A thin team-colored glow on the silhouette. It needs no painted mask, so every
// ship gets readable team colors at RTS zoom even before artists paint masks.
//   fresnel = 1 - N.V is 0 where the surface faces the camera and 1 at grazing edges;
//   pow() narrows it into a rim.
half3 GetTeamRim(half3 normalWS, half3 viewDirectionWS, half4 teamColor)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = pow(fresnel, _TeamRimPower) * _TeamRimStrength;
    return teamColor.rgb * rim * teamColor.a;
}

// Engines and window lights take on the team color: we keep the emission's
// brightness and blend its color toward the team color.
half3 ApplyTeamEmission(half3 emission, half4 teamColor)
{
    half brightness = max(emission.r, max(emission.g, emission.b));
    half3 teamEmission = teamColor.rgb * brightness;
    return lerp(emission, teamEmission, _TeamEmissionTint * teamColor.a);
}

#endif // SHIP_LIT_INPUT_INCLUDED
