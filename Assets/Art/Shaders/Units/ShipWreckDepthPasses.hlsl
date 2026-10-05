// ShipWreckDepthPasses.hlsl
// -----------------------------------------------------------------------------
// Shadow, depth and depth-normals passes of "EmpireAtWar/Ship Wreck".
// They apply the same chunk deformation and dissolve clip as the color pass; otherwise the
// intact ship would keep casting its shadow and writing depth for SSAO.
// -----------------------------------------------------------------------------
#ifndef SHIP_WRECK_DEPTH_PASSES_INCLUDED
#define SHIP_WRECK_DEPTH_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "ShipWreckDeformation.hlsl"

// Set by URP's ShadowUtils for the light currently rendering its shadow map.
float3 _LightDirection;
float3 _LightPosition;

struct WreckDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WreckDepthVaryings
{
    float4 positionCS     : SV_POSITION;
    float2 uv             : TEXCOORD0;
    half3 normalWS        : TEXCOORD1;
    half4 tangentWS       : TEXCOORD2;
    float4 restPositionOS : TEXCOORD3;   // xyz = undeformed position, w = part id (fractional on torn triangles)
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

WreckDepthVaryings WreckDepthVertexCommon(WreckDepthAttributes input, out float3 positionWS)
{
    WreckDepthVaryings output = (WreckDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float3 tangentOS = input.tangentOS.xyz;
    float part = ApplyWreckDeformation(positionOS, normalOS, tangentOS);

    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, float4(tangentOS, input.tangentOS.w));
    positionWS = TransformObjectToWorld(positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.restPositionOS = float4(input.positionOS.xyz, part);
    return output;
}

WreckDepthVaryings ShipWreckShadowVertex(WreckDepthAttributes input)
{
    float3 positionWS;
    WreckDepthVaryings output = WreckDepthVertexCommon(input, positionWS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, output.normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(positionCS);
    return output;
}

WreckDepthVaryings ShipWreckDepthVertex(WreckDepthAttributes input)
{
    float3 positionWS;
    return WreckDepthVertexCommon(input, positionWS);
}

half4 ShipWreckShadowFragment(WreckDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipWreck(input.restPositionOS.xyz, input.restPositionOS.w, input.uv);
    return 0;
}

half ShipWreckDepthFragment(WreckDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ClipWreck(input.restPositionOS.xyz, input.restPositionOS.w, input.uv);
    return input.positionCS.z;
}

half4 ShipWreckDepthNormalsFragment(WreckDepthVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ClipWreck(input.restPositionOS.xyz, input.restPositionOS.w, input.uv);

    half3 normalWS = IS_FRONT_VFACE(frontFace, true, false) ? input.normalWS : -input.normalWS;
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
    half3 bitangentWS = input.tangentWS.w * cross(normalWS, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, normalWS);
    return half4(NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld)), 0.0h);
}

#endif // SHIP_WRECK_DEPTH_PASSES_INCLUDED
