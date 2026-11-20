// ShipHullDamage.hlsl
// -----------------------------------------------------------------------------
// Star-shaped soot scorch marks with faint glowing lines along their rays that pile up as a unit loses hull.
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
float4 _HullDamageSurface;   // x = lowest middle soot opacity, y = edge breakup (radii), z = smallest / largest mark radius, w = highest middle soot opacity
float4 _HullDamageCore;      // x = soot opacity near the outline, y = star ray strength, z = mark size (0..1) where rays start, w = glowing line length (share of the outline)
half4 _HullDamageSootColor;
half4 _HullDamageEmberColor;   // rgb = glowing line colour x strength (linear), a = line width (mark radii)

#define HULL_DAMAGE_UINT_TO_UNIT (1.0 / 4294967296.0)
#define HULL_DAMAGE_TWO_PI 6.2831853

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

// Distance from a mark's centre in outline units: 1 is the outline. Marks are stars: 5-8 sharp rays of random
// length (0.7-1 of the radius) with the gaps between them pulled in to 0.25. Marks below starMinSize stay round.
// planar: offset from the mark's centre along the surface. size01: 0 = smallest mark, 1 = largest.
// rayOffset: signed distance from the nearest ray's centre line, in mark radii (large for round marks).
float GetHullDamageShapeDistance(float3 planar, float3 normalOS, float radius, float size01, float4 random,
    out float rayOffset)
{
    float distance = length(planar) / radius;
    rayOffset = 1e4;
    float spikes = _HullDamageCore.y * smoothstep(_HullDamageCore.z - 0.15, _HullDamageCore.z, size01);
    if (spikes <= 0.0 || distance >= 1.5)
        return distance;

    // Angle around the centre, in a frame lying on the surface.
    float3 axis = abs(normalOS.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
    float3 tangent = normalize(cross(axis, normalOS));
    float3 bitangent = cross(normalOS, tangent);
    float rays = 5.0 + floor(frac(random.w * 7.31) * 4.0);
    float turns = (atan2(dot(planar, bitangent), dot(planar, tangent)) / HULL_DAMAGE_TWO_PI + random.x) * rays;
    float across = frac(turns + 0.5) - 0.5;                               // -0.5..0.5 across one ray
    float rayIndex = floor(turns + 0.5);
    float rayLength = lerp(0.7, 1.0, frac(sin(rayIndex * 12.9898 + random.y * 78.233) * 43758.5453));
    float onRay = pow(saturate(1.0 - abs(across) * 2.0), 3.0);           // 1 on the ray, 0 halfway to the next
    float outline = lerp(1.0, lerp(0.25, rayLength, onRay), spikes);
    rayOffset = across * (HULL_DAMAGE_TWO_PI / rays) * distance;          // arc length to the ray's centre line
    return distance / outline;
}

// The two nearest active marks: x/z = outline distance (1 = outline; large when none), y/w = each mark's own
// random number (0..1) for its opacity. nearestRayOffset: the nearest mark's rayOffset. Two, so overlapping marks blend instead of meeting at a hard seam.
// A mark lies on the surface: its spot must be within one radius of the surface, and the distance is measured
// along the surface, so a spot just under the plating still leaves a full-size mark.
// A mark reaches at most one cell from its spot (clamped in HullDamageService), so the 3x3x3 cells around
// this point are enough.
float4 GetNearestHullDamageMarks(float3 cellPosition, float3 normalOS, half level, out float nearestRayOffset)
{
    float activeShare = level * _HullDamageParams.w;
    float maxRadius = _HullDamageParams.z;
    int3 firstCell = (int3)floor(cellPosition) - 1;
    float4 nearest = float4(1e4, 0.0, 1e4, 0.0);
    nearestRayOffset = 1e4;
    for (int z = 0; z < 3; z++)
    for (int y = 0; y < 3; y++)
    for (int x = 0; x < 3; x++)
    {
        int3 cell = firstCell + int3(x, y, z);
        float4 random = (float4)HullDamageHash(cell) * HULL_DAMAGE_UINT_TO_UNIT;
        // random.w: the share of damage at which this cell's mark appears.
        if (random.w >= activeShare)
            continue;
        float size01 = frac(random.w * 61.7);
        float radius = maxRadius * lerp(_HullDamageSurface.z, 1.0, size01);
        float3 offset = cellPosition - (float3(cell) + random.xyz);
        float depth = dot(offset, normalOS);
        if (abs(depth) >= radius)
            continue;
        float rayOffset;
        float distance = GetHullDamageShapeDistance(offset - depth * normalOS, normalOS, radius, size01, random,
            rayOffset);
        float2 found = float2(distance, frac(random.w * 113.9));
        if (distance < nearest.x)
        {
            nearest = float4(found, nearest.xy);
            nearestRayOffset = rayOffset;
        }
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

// Smooth 3D value noise, 0..1. Continuous everywhere, so outlines and lines have no blocks or tiling.
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

// Soot of one mark at this pixel. distance is the outline distance, already frayed by the edge noise.
// Fully opaque at the centre, the mark's own random opacity through the middle, light near the outline.
half GetHullDamageSoot(float distance, float random)
{
    // Squaring the random number keeps most marks near the lowest middle opacity; very dark ones are rare.
    half middle = lerp(_HullDamageSurface.x, _HullDamageSurface.w, random * random);
    half opacity = lerp(1.0h, middle, smoothstep(0.1h, 0.35h, distance));
    opacity = lerp(opacity, _HullDamageCore.x, smoothstep(0.55h, 0.85h, distance));
    return opacity * (1.0h - smoothstep(0.8h, 1.0h, distance)) * saturate(_HullDamageOpacityScale);
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
    float rayOffset;
    float4 marks = GetNearestHullDamageMarks(cellPosition, TransformWorldToObjectDir(normalWS), level, rayOffset);
    // Most of the hull is far from any mark: skip the rest there.
    UNITY_BRANCH
    if (marks.x >= 1.0 + _HullDamageSurface.y)
    {
        return;
    }

    // Two octaves of smooth noise wave the outline into an irregular stain.
    float3 noisePosition = cellPosition * _HullDamageParams.x;
    float noise = 0.65 * HullDamageValueNoise(noisePosition) + 0.35 * HullDamageValueNoise(noisePosition * 2.3 + 11.7);
    float fray = (noise - 0.5) * 2.0 * _HullDamageSurface.y;
    // Overlapping marks stack like two layers of soot.
    half soot = 1.0h - (1.0h - GetHullDamageSoot(marks.x + fray, marks.y))
        * (1.0h - GetHullDamageSoot(marks.z + fray, marks.w));

    // Burnt paint: thin, faint glowing lines along the nearest mark's rays, from the centre to part-way out.
    // Smooth noise bends them so they read as cracks, not spokes.
    float wiggle = (HullDamageValueNoise(noisePosition * 3.3 + 5.1) - 0.5) * 2.0 * _HullDamageEmberColor.a;
    half crack = 1.0h - smoothstep(0.3h * _HullDamageEmberColor.a, _HullDamageEmberColor.a, abs(rayOffset + wiggle));
    // Brightest just off the centre, fading out toward the tip; the centre itself stays dark.
    half along = smoothstep(0.03h, 0.1h, marks.x) * (1.0h - smoothstep(0.0h, _HullDamageCore.w, marks.x));
    half ember = crack * along;

    surface.albedo = lerp(surface.albedo, _HullDamageSootColor.rgb, soot);
    // Soot kills the metal sheen too; otherwise reflections turn a black mark grey.
    surface.metallic *= 1.0h - soot;
    surface.smoothness = lerp(surface.smoothness, 0.1h, soot);   // soot is matte
    surface.occlusion *= 1.0h - 0.5h * soot;
    // Running lights under soot dim with it.
    surface.emission *= 1.0h - soot;
    surface.emission += _HullDamageEmberColor.rgb * ember;
}

#endif // SHIP_HULL_DAMAGE_INCLUDED
