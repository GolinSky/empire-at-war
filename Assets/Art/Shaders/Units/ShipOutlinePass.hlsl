#ifndef SHIP_OUTLINE_PASS_INCLUDED
#define SHIP_OUTLINE_PASS_INCLUDED

// Shared tuning comes from UnitHighlightService; renderer user values select the state.
// Keeping these outside UnityPerMaterial preserves the existing shared-material batching.
half4 _UnitOutlineHoverColor;
half4 _UnitOutlineSelectedColor;
half4 _UnitOutlineAttackColor;
float _UnitOutlineWidth;

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
    UNITY_VERTEX_OUTPUT_STEREO
};

half4 GetOutlineColor()
{
    uint flags = unity_RendererUserValue >> HIGHLIGHT_USER_VALUE_SHIFT;
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

    // Collapse inactive geometry before rasterization; no per-pixel hull work is needed.
    if (output.color.a <= 0.0h || _UnitOutlineWidth <= 0.0)
    {
        output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
        return output;
    }

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 positionCS = TransformWorldToHClip(positionWS);
    float4 normalCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0.0));

    // Project the normal's derivative, then normalize in pixel space. This handles
    // perspective/orthographic cameras and keeps the width stable as the camera zooms.
    float2 screenSize = GetScaledScreenParams().xy;
    float2 directionPixels = (normalCS.xy * positionCS.w - positionCS.xy * normalCS.w) * screenSize;
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
    return half4(MixFog(input.color.rgb, input.fogFactor), input.color.a);
}

#endif // SHIP_OUTLINE_PASS_INCLUDED
