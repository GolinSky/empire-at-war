// ShipWreckForwardPass.hlsl
// -----------------------------------------------------------------------------
// Color pass of "EmpireAtWar/Ship Wreck". Reuses the Ship Lit vertex and surface code so the
// wreck looks exactly like the ship at the moment of the swap, then adds:
//   * the cut into parts and their motion (ShipWreckDeformation.hlsl)
//   * glowing torn edges and a hot interior on back faces, cooling over time
//   * a noise dissolve with a burning edge at the end of the wreck's life
// Torn parts are open shells, so the pass renders both faces (Cull Off in the shader).
// -----------------------------------------------------------------------------
#ifndef SHIP_WRECK_FORWARD_PASS_INCLUDED
#define SHIP_WRECK_FORWARD_PASS_INCLUDED

#include "ShipLitForwardPass.hlsl"
#include "ShipWreckDeformation.hlsl"

struct WreckAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Same fields as Varyings (ShipLitForwardPass.hlsl) plus the wreck data.
struct WreckVaryings
{
    float4 positionCS             : SV_POSITION;
    float2 uv                     : TEXCOORD0;
    float3 positionWS             : TEXCOORD1;
    half3 normalWS                : TEXCOORD2;
    half4 tangentWS               : TEXCOORD3;
    nointerpolation half4 teamColor : TEXCOORD4;
    half fogFactor                : TEXCOORD5;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLighting          : TEXCOORD6;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord            : TEXCOORD7;
#endif
    float3 restPositionOS         : TEXCOORD8;   // undeformed position: dissolve pattern and torn edge stick to the hull
    float part                    : TEXCOORD9;   // interpolated on purpose: fractional on triangles torn by a cut
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

WreckVaryings ShipWreckPassVertex(WreckAttributes input)
{
    // Must run before the deformation reads the object matrix (instanced / GPU Resident Drawer draws).
    UNITY_SETUP_INSTANCE_ID(input);
    Attributes lit = (Attributes)0;
    lit.positionOS = input.positionOS;
    lit.normalOS = input.normalOS;
    lit.tangentOS = input.tangentOS;
    lit.texcoord = input.texcoord;
    float part = ApplyWreckDeformation(lit.positionOS.xyz, lit.normalOS, lit.tangentOS.xyz);
    UNITY_TRANSFER_INSTANCE_ID(input, lit);

    Varyings litOutput = ShipLitPassVertex(lit);

    WreckVaryings output = (WreckVaryings)0;
    UNITY_TRANSFER_INSTANCE_ID(litOutput, output);
    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(litOutput, output);
    output.positionCS = litOutput.positionCS;
    output.uv = litOutput.uv;
    output.positionWS = litOutput.positionWS;
    output.normalWS = litOutput.normalWS;
    output.tangentWS = litOutput.tangentWS;
    output.teamColor = litOutput.teamColor;
    output.fogFactor = litOutput.fogFactor;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLighting = litOutput.vertexLighting;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = litOutput.shadowCoord;
#endif
    output.restPositionOS = input.positionOS.xyz;
    output.part = part;
    return output;
}

Varyings ToLitVaryings(WreckVaryings input, bool isFrontFace)
{
    Varyings lit = (Varyings)0;
    lit.positionCS = input.positionCS;
    lit.uv = input.uv;
    lit.positionWS = input.positionWS;
    // The inside of an open chunk is lit as if it faced the camera.
    lit.normalWS = isFrontFace ? input.normalWS : -input.normalWS;
    lit.tangentWS = input.tangentWS;
    lit.teamColor = input.teamColor;
    lit.fogFactor = input.fogFactor;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    lit.vertexLighting = input.vertexLighting;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    lit.shadowCoord = input.shadowCoord;
#endif
    return lit;
}

half4 ShipWreckPassFragment(WreckVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half dissolveEdge = ClipWreck(input.restPositionOS, input.part, input.uv);
    bool isFrontFace = IS_FRONT_VFACE(frontFace, true, false);
    Varyings lit = ToLitVaryings(input, isFrontFace);

    SurfaceData surface = CreateSurfaceData(lit);
    InputData inputData = CreateInputData(lit, surface.normalTS);

    half heat = GetWreckHeat();
    half flicker = 0.8h + 0.2h * sin(_Time.y * 17.0 + GetPartRandom(round(input.part)) * 60.0);
    // Burnt hull: the paint darkens as the heat leaves, running lights go out.
    surface.albedo *= lerp(0.35h, 1.0h, heat);
    surface.emission *= heat;
    // Glowing metal along the torn edges.
    half tornEdge = GetTornEdge(input.restPositionOS);
    surface.emission += _WreckHeatColor.rgb * (tornEdge * tornEdge * heat * flicker);
    if (!isFrontFace)
    {
        // The inside of a chunk, seen through the cracks: glowing hot, then dark.
        surface.albedo *= 0.25h;
        surface.emission += _WreckHeatColor.rgb * (0.35h * heat * flicker);
    }
    else
    {
        surface.emission += GetTeamRim(inputData.normalWS, inputData.viewDirectionWS, lit.teamColor) * heat;
    }

    surface.emission += _WreckDissolveEdgeColor.rgb * dissolveEdge;

    half4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = 1.0h;
    return color;
}

#endif // SHIP_WRECK_FORWARD_PASS_INCLUDED
