// ShipLitForwardPass.hlsl
// -----------------------------------------------------------------------------
// The pass that actually lights a ship ("UniversalForward").
//
// Data flow of a draw call:
//   mesh vertices --(Attributes)--> vertex shader --(Varyings, interpolated)--> fragment shader --> pixel
//
// We fill URP's two standard structs and let URP do the lighting math:
//   SurfaceData : what the surface IS   (albedo, metallic, smoothness, normal, emission, ...)
//   InputData   : where it is and how it is seen (position, normal, view direction, shadows, fog, ...)
// UniversalFragmentPBR(InputData, SurfaceData) then applies the main light, additional
// lights, shadows, ambient light, reflections and SSAO - the same physically based
// lighting URP/Lit uses, so converted ships look like they did before.
// -----------------------------------------------------------------------------
#ifndef SHIP_LIT_FORWARD_PASS_INCLUDED
#define SHIP_LIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Per-vertex data read from the mesh. The ": POSITION" style names are "semantics":
// they tell the GPU which mesh channel feeds each field.
struct Attributes
{
    float4 positionOS : POSITION;   // OS = object space (the mesh's own coordinates)
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;    // w = handedness of the tangent frame (+1 or -1)
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID  // lets instanced draws know which instance this vertex belongs to
};

// Data the vertex shader hands to the fragment shader. The GPU interpolates every
// field across the triangle, except fields marked nointerpolation.
struct Varyings
{
    float4 positionCS             : SV_POSITION; // CS = clip space; required output of every vertex shader
    float2 uv                     : TEXCOORD0;
    float3 positionWS             : TEXCOORD1;   // WS = world space
    half3 normalWS                : TEXCOORD2;
    half4 tangentWS               : TEXCOORD3;   // xyz = tangent, w = handedness
    // The whole ship shares one team color, so interpolating it would be wasted work.
    nointerpolation half4 teamColor : TEXCOORD4;
    half fogFactor                : TEXCOORD5;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLighting          : TEXCOORD6;   // cheap per-vertex extra lights on low quality settings
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord            : TEXCOORD7;   // only needed when shadows cannot be resolved per pixel
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO                   // VR single-pass support; costs nothing otherwise
};

Varyings ShipLitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;               // zero everything so no field is left undefined
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // URP helpers that move the vertex and its normal/tangent into world and clip space
    // (they also handle GPU Resident Drawer / DOTS instancing transparently).
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);   // applies the material's tiling/offset
    output.normalWS = normalInputs.normalWS;

    // Mirrored (negatively scaled) meshes flip the tangent frame; GetOddNegativeScale() corrects it.
    real tangentSign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInputs.tangentWS, tangentSign);

    // Read once per vertex instead of once per pixel.
    output.teamColor = GetTeamColor();
    output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLighting = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(positionInputs);
#endif
    return output;
}

// Builds "what the surface is" from the material textures.
// Every map is always sampled: the defaults (white / bump / black) make a missing map
// neutral, so the shader needs no per-material keywords and every ship material uses
// the same compiled shader variant - which is what lets the SRP Batcher group them.
SurfaceData CreateSurfaceData(Varyings input)
{
    SurfaceData surface = (SurfaceData)0;

    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
    half teamMask = SAMPLE_TEXTURE2D(_TeamMaskMap, sampler_BaseMap, input.uv).r;
    surface.albedo = ApplyTeamMask(albedoAlpha.rgb, teamMask, input.teamColor);
    surface.alpha = 1.0h;                        // ships are opaque

    half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_BaseMap, input.uv);
    surface.metallic = metallicGloss.r * _Metallic;
    surface.smoothness = metallicGloss.a * _Smoothness;
    surface.specular = half3(0.0h, 0.0h, 0.0h); // metallic workflow: specular is derived from albedo

    // Normal maps store directions packed into 0..1 colors; UnpackNormalScale turns them
    // back into a tangent-space direction and scales the bumpiness.
    half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, input.uv);
    surface.normalTS = UnpackNormalScale(packedNormal, _BumpScale);

    half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, input.uv).g;
    surface.occlusion = lerp(1.0h, occlusion, _OcclusionStrength);

    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, input.uv).rgb * _EmissionColor.rgb;
    surface.emission = ApplyTeamEmission(emission, input.teamColor);
    return surface;
}

// Builds "where the surface is and how it is seen" for URP's lighting functions.
InputData CreateInputData(Varyings input, half3 normalTS)
{
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;

    // Tangent frame: tangent, bitangent, normal. Multiplying a tangent-space normal by it
    // gives the same normal in world space.
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
    inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, inputData.tangentToWorld));
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
#endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.vertexLighting = input.vertexLighting;
#endif
    // Ships move, so they never use baked lightmaps: ambient light comes from the
    // light probes / skybox as spherical harmonics, evaluated per pixel.
    inputData.bakedGI = SampleSH(inputData.normalWS);
    // Screen UV is needed for SSAO and the Forward+ light list lookup.
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
    return inputData;
}

half4 ShipLitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surface = CreateSurfaceData(input);
    InputData inputData = CreateInputData(input, surface.normalTS);

    // The rim is added as emission so it glows the same in light and in shadow.
    surface.emission += GetTeamRim(inputData.normalWS, inputData.viewDirectionWS, input.teamColor);

    half4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = 1.0h;
    return color;
}

#endif // SHIP_LIT_FORWARD_PASS_INCLUDED
