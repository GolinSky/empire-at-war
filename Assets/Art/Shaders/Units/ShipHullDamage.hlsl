// ShipHullDamage.hlsl
// -----------------------------------------------------------------------------
// Scorch marks, burnt plating and glowing edges that spread as a unit loses hull.
//
// How it stays cheap and batchable
//   * The damage level (0-15) and a pattern seed live in the renderer user value
//     (bits 11-19, see ShipLitInput.hlsl). No material, keyword or property block changes.
//   * Detail comes from one shared texture and a few globals (HullDamageService.cs).
//   * Level 0 skips the whole block: one branch, constant for the whole draw.
//
// How the pattern grows
//   A fixed noise field is compared against a threshold that drops as the level rises.
//   Higher levels only lower the threshold, so existing marks never move: they grow,
//   and new ones appear. Repair raises the threshold again and the marks shrink.
//   The field lives in object space scaled to world units, so it sticks to the hull
//   (turrets carry their own marks) and a mark is about the same size on every ship.
// -----------------------------------------------------------------------------
#ifndef SHIP_HULL_DAMAGE_INCLUDED
#define SHIP_HULL_DAMAGE_INCLUDED

// R = height, G = cracks, B = roughness, A = ember mask. Tiles in every direction.
TEXTURE2D(_HullDamageMap);
SAMPLER(sampler_HullDamageMap);
float4 _HullDamageParams;    // x = detail repeats per world unit, y = 1 / patch size (world units), z = edge width, w = height blend
float4 _HullDamageSurface;   // x = normal strength, y = ember strength, z = coverage at full damage, w = ember flicker speed
half4 _HullDamageCharColor;  // burnt paint
half4 _HullDamageMetalColor; // bare metal in the middle of a burn
half4 _HullDamageEmberColor; // glowing edge (HDR)
float _HullDamageQuality;    // QualitySettings level; below FULL_QUALITY_LEVEL: one planar sample, no embers

#define HULL_DAMAGE_FULL_QUALITY_LEVEL 2.0

// Object-space position in world units, shifted per renderer so ships of one class differ.
float3 GetHullDamagePosition(float3 positionOS)
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float3 scale = float3(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));
    float seed = (float)GetHullDamageSeed();
    return positionOS * scale + seed * float3(37.31, 53.17, 71.93);
}

float HullDamageHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Smooth 3D value noise, 0..1.
float HullDamageValueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float x00 = lerp(HullDamageHash(cell), HullDamageHash(cell + float3(1, 0, 0)), u.x);
    float x10 = lerp(HullDamageHash(cell + float3(0, 1, 0)), HullDamageHash(cell + float3(1, 1, 0)), u.x);
    float x01 = lerp(HullDamageHash(cell + float3(0, 0, 1)), HullDamageHash(cell + float3(1, 0, 1)), u.x);
    float x11 = lerp(HullDamageHash(cell + float3(0, 1, 1)), HullDamageHash(cell + float3(1, 1, 1)), u.x);
    return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
}

half4 SampleHullDamageDetail(float2 uv, float2 uvDdx, float2 uvDdy)
{
    return SAMPLE_TEXTURE2D_GRAD(_HullDamageMap, sampler_HullDamageMap, uv, uvDdx, uvDdy);
}

// level: 0 = intact (skipped), 1 = destroyed.
void ApplyHullDamage(inout SurfaceData surface, float3 positionDamage, half level, float3 positionWS,
    half3 normalWS, half4 tangentWS)
{
    // Derivatives first: they are only valid outside the branch below.
    float3 uvw = positionDamage * _HullDamageParams.x;
    float3 uvwDdx = ddx(uvw);
    float3 uvwDdy = ddy(uvw);
    float3 positionDdx = ddx(positionWS);
    float3 positionDdy = ddy(positionWS);

    // One user value per draw, so every pixel of the draw takes the same side.
    UNITY_BRANCH
    if (level <= 0.0h)
    {
        return;
    }

    half3 normalWSUnit = normalize(normalWS);
    float3 normalOS = TransformWorldToObjectDir(normalWSUnit);
    float3 blend = pow(abs(normalOS), 4.0);
    blend /= max(blend.x + blend.y + blend.z, 1e-4);

    bool fullQuality = _HullDamageQuality >= HULL_DAMAGE_FULL_QUALITY_LEVEL;
    half4 detail;
    if (fullQuality)
    {
        detail = SampleHullDamageDetail(uvw.zy, uvwDdx.zy, uvwDdy.zy) * blend.x
            + SampleHullDamageDetail(uvw.xz, uvwDdx.xz, uvwDdy.xz) * blend.y
            + SampleHullDamageDetail(uvw.xy, uvwDdx.xy, uvwDdy.xy) * blend.z;
    }
    else
    {
        // Cheapest projection: the plane the surface faces most.
        bool useX = blend.x >= blend.y && blend.x >= blend.z;
        bool useY = !useX && blend.y >= blend.z;
        float2 uv = useX ? uvw.zy : (useY ? uvw.xz : uvw.xy);
        float2 uvDdx = useX ? uvwDdx.zy : (useY ? uvwDdx.xz : uvwDdx.xy);
        float2 uvDdy = useX ? uvwDdy.zy : (useY ? uvwDdy.xz : uvwDdy.xy);
        detail = SampleHullDamageDetail(uv, uvDdx, uvDdy);
    }

    // Patch field: two octaves of noise in world-sized cells.
    float3 patchPosition = positionDamage * _HullDamageParams.y;
    float field = 0.65 * HullDamageValueNoise(patchPosition)
        + 0.35 * HullDamageValueNoise(patchPosition * 2.13 + 17.1);
    // The detail height breaks up the edge, so burns end in ragged plating, not a soft gradient.
    field += (detail.r - 0.5) * _HullDamageParams.w;

    half edgeWidth = max(_HullDamageParams.z, 1e-3);
    float threshold = 1.0 - level * _HullDamageSurface.z;
    half burn = saturate((field - threshold) / edgeWidth);
    half core = saturate((field - threshold - edgeWidth) / edgeWidth);

    // Burnt paint, then bare scorched metal in the middle; cracks darken both.
    half cracks = detail.g * burn;
    half3 charred = _HullDamageCharColor.rgb * lerp(0.6h, 1.0h, detail.r);
    surface.albedo = lerp(surface.albedo, charred, burn);
    surface.albedo = lerp(surface.albedo, _HullDamageMetalColor.rgb, core);
    surface.albedo *= 1.0h - 0.6h * cracks;
    surface.metallic = lerp(lerp(surface.metallic, 0.1h, burn), 0.85h, core);
    surface.smoothness = lerp(surface.smoothness, (1.0h - detail.b) * 0.45h, burn);
    surface.occlusion *= 1.0h - 0.35h * burn - 0.3h * cracks;

    // Bump from the detail height (surface gradient), only inside the burn.
    half height = detail.r - 0.5h * detail.g;
    float heightDdx = ddx(height);
    float heightDdy = ddy(height);
    float3 r1 = cross(positionDdy, normalWSUnit);
    float3 r2 = cross(normalWSUnit, positionDdx);
    float det = dot(positionDdx, r1);
    float3 heightGradient = (heightDdx * r1 + heightDdy * r2) * (sign(det) / max(abs(det), 1e-8));
    half3 bitangentWS = tangentWS.w * cross(normalWSUnit, tangentWS.xyz);
    half2 gradientTS = half2(dot(heightGradient, tangentWS.xyz), dot(heightGradient, bitangentWS));
    surface.normalTS = normalize(half3(surface.normalTS.xy - gradientTS * (_HullDamageSurface.x * burn),
        surface.normalTS.z));

    // Running lights burn out with the plating.
    surface.emission *= 1.0h - burn;
    if (fullQuality)
    {
        // Embers along the edge of each burn and in its cracks, flickering slowly.
        // 4b(1-b) is 0 outside the burn and where it is complete, 1 halfway through its edge.
        half edgeBand = 4.0h * burn * (1.0h - burn);
        half flicker = 0.75h + 0.25h * sin(_Time.y * _HullDamageSurface.w + field * 40.0);
        half ember = (edgeBand + cracks * core) * detail.a * flicker * _HullDamageSurface.y;
        surface.emission += _HullDamageEmberColor.rgb * ember;
    }
}

#endif // SHIP_HULL_DAMAGE_INCLUDED
