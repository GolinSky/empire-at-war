// ShipIonStunPass.hlsl
// -----------------------------------------------------------------------------
// Ion stun: unstable blue-white electricity crawling over the hull while the unit's systems are down.
//
// Why a separate pass instead of code in ShipLitForwardPass
//   * UnitIonStunRenderPass draws this pass only for renderers on the ion stun rendering layer
//     (UnitRenderStateComponent sets it while the unit is ion disabled). Unstunned ships never run it,
//     and the forward pass keeps its instruction count and registers.
//   * No keywords and no material changes: it shares UnityPerMaterial with the other passes (SRP Batcher)
//     and reads only the per-renderer pattern seed already in the user value.
//
// Look
//   Veins are the zero crossings of 3D noise cut by the hull surface, so they wander across panels
//   instead of following UVs. A smooth drift makes them crawl; a second, finer layer re-rolls ~20 times
//   per second for the crackle. A slow moving mask keeps only patches of the hull live at once.
//   Line width is measured in pixels (fwidth), so arcs stay thin and readable at any zoom.
// -----------------------------------------------------------------------------
#ifndef SHIP_ION_STUN_PASS_INCLUDED
#define SHIP_ION_STUN_PASS_INCLUDED

#include "ShipHullDamage.hlsl"

#define ION_STUN_VEIN_FREQUENCY (1.0 / 14.0)     // vein layer, per world unit
#define ION_STUN_SPARK_FREQUENCY (1.0 / 5.5)     // crackle layer, per world unit
#define ION_STUN_MASK_FREQUENCY (1.0 / 40.0)     // live patches, per world unit
#define ION_STUN_CRAWL_SPEED 0.9                 // noise units per second
#define ION_STUN_CRACKLE_RATE 18.0               // crackle re-rolls per second
#define ION_STUN_CORE_WIDTH 1.1                  // pixels
#define ION_STUN_GLOW_WIDTH 4.0                  // pixels
#define ION_STUN_CORE_COLOR half3(3.2, 3.8, 4.5) // HDR, near white so bloom haloes it
#define ION_STUN_GLOW_COLOR half3(0.25, 0.75, 2.4)
#define ION_STUN_RIM_COLOR half3(0.05, 0.22, 0.6)

struct IonStunAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct IonStunVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionPattern : TEXCOORD0;   // object space in world units, offset per unit (ShipHullDamage.hlsl)
    half3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    half fogFactor : TEXCOORD3;
    UNITY_VERTEX_OUTPUT_STEREO
};

IonStunVaryings ShipIonStunVertex(IonStunAttributes input)
{
    IonStunVaryings output = (IonStunVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.positionPattern = GetHullDamagePosition(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
    return output;
}

// 1 on the noise's mid-level crossing, falling to 0 widthPixels away from it on screen.
half GetIonVein(float noise, float widthPixels)
{
    float distance = abs(noise - 0.5);
    return (half)saturate(1.0 - distance / max(fwidth(noise) * widthPixels, 1e-5));
}

half4 ShipIonStunFragment(IonStunVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float time = _Time.y;
    float crackleStep = floor(time * ION_STUN_CRACKLE_RATE);
    float3 crackleJump = float3(HullDamageNoiseHash(float3(crackleStep, 1.7, 0.0)),
        HullDamageNoiseHash(float3(crackleStep, 5.3, 2.0)), HullDamageNoiseHash(float3(crackleStep, 9.1, 4.0))) * 7.0;
    float3 crawl = float3(0.6, 1.0, -0.8) * (time * ION_STUN_CRAWL_SPEED);
    float3 p = input.positionPattern;

    float veinNoise = HullDamageValueNoise(p * ION_STUN_VEIN_FREQUENCY + crawl);
    float sparkNoise = HullDamageValueNoise(p * ION_STUN_SPARK_FREQUENCY + crackleJump);
    half mask = (half)smoothstep(0.42, 0.68, HullDamageValueNoise(p * ION_STUN_MASK_FREQUENCY - crawl * 0.35));

    // Sparks only fork off near a vein, so the crackle reads as branches of one arc, not static.
    half vein = GetIonVein(veinNoise, ION_STUN_CORE_WIDTH);
    half veinGlow = GetIonVein(veinNoise, ION_STUN_GLOW_WIDTH);
    half nearVein = (half)saturate(1.0 - abs(veinNoise - 0.5) * 9.0);
    half spark = GetIonVein(sparkNoise, ION_STUN_CORE_WIDTH * 0.8) * nearVein;

    // Whole-hull flicker, re-rolled with the crackle: the field browns out and surges.
    half flicker = 0.6h + 0.4h * (half)HullDamageNoiseHash(float3(crackleStep, 3.3, 7.7));
    half3 viewDirectionWS = (half3)GetWorldSpaceNormalizeViewDir(input.positionWS);
    half rim = pow(1.0h - saturate(abs(dot(normalize(input.normalWS), viewDirectionWS))), 3.0h);

    half3 color = (ION_STUN_CORE_COLOR * max(vein, spark) + ION_STUN_GLOW_COLOR * veinGlow) * mask * flicker
        + ION_STUN_RIM_COLOR * rim * flicker;
    // Additive: fog fades the arcs out instead of tinting them.
    color = MixFogColor(color, half3(0.0h, 0.0h, 0.0h), input.fogFactor);
    return half4(color, 0.0h);
}

#endif // SHIP_ION_STUN_PASS_INCLUDED
