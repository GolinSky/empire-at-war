// ShipHullDamage.hlsl
// -----------------------------------------------------------------------------
// Soot stains and blistered paint that pile up as a unit loses hull.
//
// How it stays cheap and batchable
//   * The damage level (0-15) and a pattern seed live in the renderer user value
//     (bits 11-19, see ShipLitInput.hlsl). No material, keyword or property block changes.
//   * Breakup detail comes from one shared texture and a few globals (HullDamageService.cs).
//   * Level 0 skips the whole block: one branch, constant for the whole draw.
//
// How marks appear
//   The hull is split into a 3D grid of cells. Each cell holds at most one small mark at a random
//   spot, with a random "appear at" level. Every damage step switches on the next batch of cells,
//   so a hit adds new marks elsewhere on the hull; marks never grow. Repair switches them off
//   in reverse order. The grid lives in object space scaled to world units, so marks stick to
//   the hull (turrets carry their own) and are the same size on every ship.
// -----------------------------------------------------------------------------
#ifndef SHIP_HULL_DAMAGE_INCLUDED
#define SHIP_HULL_DAMAGE_INCLUDED

// R = height, G = cracks, B = roughness, A = unused. Tiles in every direction.
TEXTURE2D(_HullDamageMap);
SAMPLER(sampler_HullDamageMap);
float4 _HullDamageParams;    // x = detail repeats per world unit, y = 1 / mark spacing (world units), z = largest mark radius (cells), w = share of cells marked at 0 hull
float4 _HullDamageSurface;   // x = lowest soot opacity, y = edge breakup (radii), z = smallest / largest mark radius, w = highest soot opacity
float4 _HullDamageCore;      // x = soot opacity at a mark's centre, y = share of the radius over which it fades to the mark's own opacity
half4 _HullDamageSootColor;
half4 _HullDamageBlisterColor; // blistered paint in the middle of a mark
float _HullDamageQuality;    // QualitySettings level; below FULL_QUALITY_LEVEL: one planar sample

#define HULL_DAMAGE_FULL_QUALITY_LEVEL 2.0
#define HULL_DAMAGE_UINT_TO_UNIT (1.0 / 4294967296.0)

// Object-space position in world units, shifted per renderer so ships of one class differ.
float3 GetHullDamagePosition(float3 positionOS)
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float3 scale = float3(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));
    float seed = (float)GetHullDamageSeed();
    return positionOS * scale + seed * float3(37.31, 53.17, 71.93);
}

// PCG4D (Jarkko & Olano 2020): four independent random numbers per cell in one go.
uint4 HullDamageHash(int3 cell)
{
    uint4 v = uint4(asuint(cell), 0x9E3779B9u) * 1664525u + 1013904223u;
    v.x += v.y * v.w; v.y += v.z * v.x; v.z += v.x * v.y; v.w += v.y * v.z;
    v ^= v >> 16u;
    v.x += v.y * v.w; v.y += v.z * v.x; v.z += v.x * v.y; v.w += v.y * v.z;
    return v;
}

// x = distance to the nearest active mark, in mark radii (1 = edge); large when no mark is near.
// y = that mark's own random number (0..1), used for its opacity.
// A mark is a disk on the surface: its spot must lie within one radius of the surface, and the distance is
// measured along the surface, so a spot just under the plating still leaves a full-size mark.
// A mark reaches at most one cell from its spot (clamped in HullDamageService), so the 3x3x3 cells around
// this point are enough.
float2 GetNearestHullDamageMark(float3 cellPosition, float3 normalOS, half level)
{
    float activeShare = level * _HullDamageParams.w;
    float maxRadius = _HullDamageParams.z;
    int3 firstCell = (int3)floor(cellPosition) - 1;
    float2 nearest = float2(1e4, 0.0);
    for (int z = 0; z < 3; z++)
    for (int y = 0; y < 3; y++)
    for (int x = 0; x < 3; x++)
    {
        int3 cell = firstCell + int3(x, y, z);
        float4 random = (float4)HullDamageHash(cell) * HULL_DAMAGE_UINT_TO_UNIT;
        // random.w: the share of damage at which this cell's mark appears.
        if (random.w >= activeShare)
            continue;
        float radius = maxRadius * lerp(_HullDamageSurface.z, 1.0, frac(random.w * 61.7));
        float3 offset = cellPosition - (float3(cell) + random.xyz);
        float depth = dot(offset, normalOS);
        if (abs(depth) >= radius)
            continue;
        float distance = length(offset - depth * normalOS) / radius;
        if (distance < nearest.x)
            nearest = float2(distance, frac(random.w * 113.9));
    }

    return nearest;
}

half4 SampleHullDamageDetail(float2 uv, float2 uvDdx, float2 uvDdy)
{
    return SAMPLE_TEXTURE2D_GRAD(_HullDamageMap, sampler_HullDamageMap, uv, uvDdx, uvDdy);
}

// level: 0 = intact (skipped), 1 = destroyed.
void ApplyHullDamage(inout SurfaceData surface, float3 positionDamage, half level, half3 normalWS)
{
    // Derivatives first: they are only valid outside the branches below.
    float3 uvw = positionDamage * _HullDamageParams.x;
    float3 uvwDdx = ddx(uvw);
    float3 uvwDdy = ddy(uvw);

    // One user value per draw, so every pixel of the draw takes the same side.
    UNITY_BRANCH
    if (level <= 0.0h)
    {
        return;
    }

    float3 normalOS = TransformWorldToObjectDir(normalWS);
    // The material's mark scale grows marks and their spacing together, so marks stay within the 27-cell search.
    float2 mark = GetNearestHullDamageMark(positionDamage * (_HullDamageParams.y / _HullDamageMarkScale),
        normalOS, level);
    // Most of an undamaged-looking hull is far from any mark: skip the texture work there.
    UNITY_BRANCH
    float markDistance = mark.x;
    if (markDistance >= 1.0 + _HullDamageSurface.y)
    {
        return;
    }

    float3 blend = pow(abs(normalOS), 4.0);
    blend /= max(blend.x + blend.y + blend.z, 1e-4);

    half4 detail;
    if (_HullDamageQuality >= HULL_DAMAGE_FULL_QUALITY_LEVEL)
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

    // The detail height frays the round edge into an irregular stain.
    float frayed = markDistance + (detail.r - 0.5) * 2.0 * _HullDamageSurface.y;
    // Soft, semi-transparent soot fading out to the edge; blistered paint only in the middle.
    // Each mark has its own opacity; squaring the random number keeps most marks near the lowest opacity
    // and makes fully dark ones rare. A darker centre fades slowly out to that opacity.
    half opacity = lerp(_HullDamageSurface.x, _HullDamageSurface.w, mark.y * mark.y);
    opacity = lerp(max(_HullDamageCore.x, opacity), opacity, smoothstep(0.0h, _HullDamageCore.y, frayed));
    half soot = (1.0h - smoothstep(0.35h, 1.0h, frayed)) * saturate(opacity * _HullDamageOpacityScale);
    half blister = (1.0h - smoothstep(0.15h, 0.45h, frayed)) * smoothstep(0.45h, 0.65h, detail.r + 0.5h * detail.g);

    surface.albedo = lerp(surface.albedo, _HullDamageSootColor.rgb, soot);
    surface.albedo = lerp(surface.albedo, _HullDamageBlisterColor.rgb, blister);
    surface.metallic = lerp(surface.metallic * (1.0h - 0.5h * soot), 0.6h, blister);
    surface.smoothness = lerp(surface.smoothness, (1.0h - detail.b) * 0.25h, max(soot, blister));
    surface.occlusion *= 1.0h - 0.25h * soot;
    // Running lights under soot dim with it.
    surface.emission *= 1.0h - soot;
}

#endif // SHIP_HULL_DAMAGE_INCLUDED
