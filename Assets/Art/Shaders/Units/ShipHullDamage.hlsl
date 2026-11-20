// ShipHullDamage.hlsl
// -----------------------------------------------------------------------------
// Soft soot scorch marks that pile up as a unit loses hull.
//
// How it stays cheap and batchable
//   * The damage level (0-15) and a pattern seed live in the renderer user value
//     (bits 11-19, see ShipLitInput.hlsl). No material, keyword or property block changes.
//   * The look comes from a few globals (HullDamageService.cs); no textures are sampled.
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

float4 _HullDamageParams;    // x = edge noise frequency (per cell), y = 1 / mark spacing (world units), z = largest mark radius (cells), w = share of cells marked at 0 hull
float4 _HullDamageSurface;   // x = lowest soot opacity, y = edge breakup (radii), z = smallest / largest mark radius, w = highest soot opacity
float4 _HullDamageCore;      // x = soot opacity at a mark's centre, y = share of the radius where the centre has faded to the mark's own opacity (solid up to half of it)
half4 _HullDamageSootColor;

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

// The two nearest active marks: x/z = distance in mark radii (1 = edge; large when none), y/w = each mark's own
// random number (0..1) for its opacity. Two, so overlapping marks blend instead of meeting at a hard seam.
// A mark is a disk on the surface: its spot must lie within one radius of the surface, and the distance is
// measured along the surface, so a spot just under the plating still leaves a full-size mark.
// A mark reaches at most one cell from its spot (clamped in HullDamageService), so the 3x3x3 cells around
// this point are enough.
float4 GetNearestHullDamageMarks(float3 cellPosition, float3 normalOS, half level)
{
    float activeShare = level * _HullDamageParams.w;
    float maxRadius = _HullDamageParams.z;
    int3 firstCell = (int3)floor(cellPosition) - 1;
    float4 nearest = float4(1e4, 0.0, 1e4, 0.0);
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
        float2 found = float2(distance, frac(random.w * 113.9));
        if (distance < nearest.x)
            nearest = float4(found, nearest.xy);
        else if (distance < nearest.z)
            nearest.zw = found;
    }

    return nearest;
}

float HullDamageNoiseHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Smooth 3D value noise, 0..1. Continuous everywhere, so mark edges wave without blocks or tiling.
float HullDamageValueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float x00 = lerp(HullDamageNoiseHash(cell), HullDamageNoiseHash(cell + float3(1, 0, 0)), u.x);
    float x10 = lerp(HullDamageNoiseHash(cell + float3(0, 1, 0)), HullDamageNoiseHash(cell + float3(1, 1, 0)), u.x);
    float x01 = lerp(HullDamageNoiseHash(cell + float3(0, 0, 1)), HullDamageNoiseHash(cell + float3(1, 0, 1)), u.x);
    float x11 = lerp(HullDamageNoiseHash(cell + float3(0, 1, 1)), HullDamageNoiseHash(cell + float3(1, 1, 1)), u.x);
    return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
}

// Soot of one mark at this pixel. distance is already frayed by the edge noise.
half GetHullDamageSoot(float distance, float random)
{
    // Each mark has its own opacity; squaring the random number keeps most marks near the lowest opacity
    // and makes fully dark ones rare. A solid centre fades slowly out to that opacity, then to the soft edge.
    half opacity = lerp(_HullDamageSurface.x, _HullDamageSurface.w, random * random);
    opacity = lerp(max(_HullDamageCore.x, opacity), opacity, smoothstep(0.5h * _HullDamageCore.y, _HullDamageCore.y, distance));
    return (1.0h - smoothstep(0.6h, 1.0h, distance)) * saturate(opacity * _HullDamageOpacityScale);
}

// level: 0 = intact (skipped), 1 = destroyed.
void ApplyHullDamage(inout SurfaceData surface, float3 positionDamage, half level, half3 normalWS)
{
    // One user value per draw, so every pixel of the draw takes the same side.
    UNITY_BRANCH
    if (level <= 0.0h)
    {
        return;
    }

    // The material's mark scale grows marks and their spacing together, so marks stay within the 27-cell search.
    float3 cellPosition = positionDamage * (_HullDamageParams.y / _HullDamageMarkScale);
    float4 marks = GetNearestHullDamageMarks(cellPosition, TransformWorldToObjectDir(normalWS), level);
    // Most of the hull is far from any mark: skip the rest there.
    UNITY_BRANCH
    if (marks.x >= 1.0 + _HullDamageSurface.y)
    {
        return;
    }

    // Two octaves of smooth noise wave the round edge into an irregular stain.
    float3 noisePosition = cellPosition * _HullDamageParams.x;
    float noise = 0.65 * HullDamageValueNoise(noisePosition) + 0.35 * HullDamageValueNoise(noisePosition * 2.3 + 11.7);
    float fray = (noise - 0.5) * 2.0 * _HullDamageSurface.y;
    // Overlapping marks stack like two layers of soot.
    half soot = 1.0h - (1.0h - GetHullDamageSoot(marks.x + fray, marks.y))
        * (1.0h - GetHullDamageSoot(marks.z + fray, marks.w));

    surface.albedo = lerp(surface.albedo, _HullDamageSootColor.rgb, soot);
    // Soot kills the metal sheen too; otherwise reflections turn a black mark grey.
    surface.metallic *= 1.0h - soot;
    surface.smoothness = lerp(surface.smoothness, 0.1h, soot);   // soot is matte
    surface.occlusion *= 1.0h - 0.5h * soot;
    // Running lights under soot dim with it.
    surface.emission *= 1.0h - soot;
}

#endif // SHIP_HULL_DAMAGE_INCLUDED
