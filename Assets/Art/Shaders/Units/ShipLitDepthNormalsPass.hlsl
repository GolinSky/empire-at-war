// ShipLitDepthNormalsPass.hlsl
// -----------------------------------------------------------------------------
// Writes the ship's world-space normal (and depth) into _CameraNormalsTexture.
// URP runs this pass only when a feature asks for normals - in this project the
// SSAO renderer feature - so shaded ships receive correct ambient occlusion.
//
// It uses the same normal-map math as the forward pass; otherwise SSAO would
// darken the hull based on normals that do not match what the player sees.
// -----------------------------------------------------------------------------
#ifndef SHIP_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define SHIP_LIT_DEPTH_NORMALS_PASS_INCLUDED

struct DepthNormalsAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    half3 normalWS    : TEXCOORD1;
    half4 tangentWS   : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

DepthNormalsVaryings ShipLitDepthNormalsVertex(DepthNormalsAttributes input)
{
    DepthNormalsVaryings output = (DepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.normalWS = normalInputs.normalWS;
    real tangentSign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInputs.tangentWS, tangentSign);
    return output;
}

half4 ShipLitDepthNormalsFragment(DepthNormalsVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // This pass never reads _BaseMap, so the compiler strips its sampler; use the normal map's own.
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
    float3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);

    // The texture stores the normal as-is (-1..1); the w channel is unused.
    return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
}

#endif // SHIP_LIT_DEPTH_NORMALS_PASS_INCLUDED
