#ifndef SHIP_OUTLINE_PASS_INCLUDED
#define SHIP_OUTLINE_PASS_INCLUDED

// Shared tuning comes from UnitHighlightService; renderer user values select the state.
// Keeping these outside UnityPerMaterial preserves the existing shared-material batching.
half4 _UnitOutlineHoverColor;
half4 _UnitOutlineSelectedColor;
half4 _UnitOutlineAttackColor;
float _UnitOutlineWidth;

// Written by the ShipOutlineOwner pass: per pixel, the owner id of the nearest highlighted unit, 0 for none.
TEXTURE2D_FLOAT(_UnitOutlineOwnerTexture);

struct OutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    nointerpolation half4 color : TEXCOORD0;
    half fogFactor : TEXCOORD1;
    nointerpolation float ownerId : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

float GetOutlineOwnerId()
{
    return (float)((unity_RendererUserValue >> OUTLINE_OWNER_USER_VALUE_SHIFT) & OUTLINE_OWNER_USER_VALUE_MASK);
}

half4 GetOutlineColor()
{
    uint flags = (unity_RendererUserValue >> HIGHLIGHT_USER_VALUE_SHIFT) & HIGHLIGHT_USER_VALUE_MASK;
    if ((flags & ATTACKED_FLAG) != 0u)
        return _UnitOutlineAttackColor;
    if ((flags & SELECTED_FLAG) != 0u)
        return _UnitOutlineSelectedColor;
    if ((flags & HOVERED_FLAG) != 0u)
        return _UnitOutlineHoverColor;
    return half4(0.0h, 0.0h, 0.0h, 0.0h);
}

OutlineVaryings ShipOutlineVertex(OutlineAttributes input)
{
    OutlineVaryings output = (OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.color = GetOutlineColor();
    output.ownerId = GetOutlineOwnerId();

    // Collapse inactive geometry before rasterization; no per-pixel hull work is needed.
    if (output.color.a <= 0.0h || _UnitOutlineWidth <= 0.0)
    {
        output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
        return output;
    }

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 positionCS = TransformWorldToHClip(positionWS);
    float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);

    // Push along the view-space normal, scaled to pixels. Unlike the projected normal
    // derivative, this direction never depends on w, so it cannot mirror for vertices behind
    // the camera. Scaling the offset by w then keeps the width exact on triangles clipped by
    // the near plane (ships only partly on screen) and stable as the camera zooms.
    float2 screenSize = GetScaledScreenParams().xy;
    float2 directionPixels = normalVS.xy * float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]) * screenSize;
    directionPixels *= rsqrt(max(dot(directionPixels, directionPixels), 1e-8));
    positionCS.xy += directionPixels * (2.0 * _UnitOutlineWidth / screenSize) * positionCS.w;
    output.positionCS = positionCS;
    output.fogFactor = ComputeFogFactor(positionCS.z);
    return output;
}

half4 ShipOutlineFragment(OutlineVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(input.color.a - 0.001h);
    // Skip only this unit's own silhouette (hard-normal seams, overlapping parts). Another highlighted
    // unit's silhouette does not hide this outline: depth already hides it where that unit is in front.
    float owner = LOAD_TEXTURE2D(_UnitOutlineOwnerTexture, uint2(input.positionCS.xy)).r;
    clip(abs(owner - input.ownerId) - 0.5);
    return half4(MixFog(input.color.rgb, input.fogFactor), input.color.a);
}

struct OutlineOwnerVaryings
{
    float4 positionCS : SV_POSITION;
    nointerpolation float ownerId : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

OutlineOwnerVaryings ShipOutlineOwnerVertex(OutlineAttributes input)
{
    OutlineOwnerVaryings output = (OutlineOwnerVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.ownerId = GetOutlineOwnerId();
    return output;
}

// Writes the unit's owner id; the pass's own depth keeps the nearest highlighted surface per pixel.
float4 ShipOutlineOwnerFragment(OutlineOwnerVaryings input) : SV_Target
{
    return float4(input.ownerId, 0.0, 0.0, 0.0);
}

#endif // SHIP_OUTLINE_PASS_INCLUDED
