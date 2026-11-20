// ShipHullDamage.hlsl
// -----------------------------------------------------------------------------
// Soot scorch marks (outline between a circle and a star), each with a sunken burnt patch whose rim glows,
// piling up as a unit loses hull.
//
// How it stays cheap and batchable
//   * The damage level (0-31) and a pattern seed live in the renderer user value
//     (bits 11-20, see ShipLitInput.hlsl). No material, keyword or property block changes.
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
float4 _HullDamageCore;      // x = soot opacity near the outline, y = star ray strength, z = mark size (0..1) where rays start, w = burnt patch radius (mark radii)
float4 _HullDamageBurnt;     // x = how far the patch centre may sit from the mark centre (mark radii), y = depth (normal tilt on the patch wall), z = pixels across the largest mark radius below which the detail fades out, w = even soot at 0 hull once marks are smaller than a pixel
float _HullDamageQuality;    // QualitySettings level; below HULL_DAMAGE_FULL_QUALITY_LEVEL: one edge-noise octave, flat patches
half4 _HullDamageSootColor;
half4 _HullDamageBurntColor;   // burnt metal inside the patch
half4 _HullDamageEmberColor;   // rgb = glow colour x strength (linear), a = width of the glowing rim

#define HULL_DAMAGE_UINT_TO_UNIT (1.0 / 4294967296.0)
#define HULL_DAMAGE_TWO_PI 6.2831853
#define HULL_DAMAGE_PATCH_THRESHOLD 0.42
#define HULL_DAMAGE_SLOPE_STEP 0.02
#define HULL_DAMAGE_FULL_QUALITY_LEVEL 2.0

// Size of the renderer along each object axis, in world units.
float3 GetHullDamageScale()
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    return float3(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));
}

// Pattern space: object-space position in world units, shifted per unit so ships of one class differ.
float3 GetHullDamagePosition(float3 positionOS)
{
    float seed = (float)GetHullDamageSeed();
    return positionOS * GetHullDamageScale() + seed * float3(37.31, 53.17, 71.93);
}

// World direction -> pattern space. Pattern space only undoes the renderer's rotation (its scale is already in
// the positions), so this stays a true surface normal on unevenly scaled parts.
float3 HullDamageWorldToPatternDir(float3 directionWS)
{
    return normalize(mul(directionWS, (float3x3)GetObjectToWorldMatrix()) / GetHullDamageScale());
}

float3 HullDamagePatternToWorldDir(float3 directionPattern)
{
    return normalize(mul((float3x3)GetObjectToWorldMatrix(), directionPattern / GetHullDamageScale()));
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

// Cheap integer hash deciding whether a cell's mark is on: the full PCG4D only runs for cells that pass it.
uint HullDamageActivationHash(int3 cell)
{
    uint3 c = asuint(cell);
    uint h = c.x * 0x8DA6B343u ^ c.y * 0xD8163841u ^ c.z * 0xCB1AB31Fu;
    h ^= h >> 16u;
    h *= 0x7FEB352Du;
    h ^= h >> 15u;
    h *= 0x846CA68Bu;
    h ^= h >> 16u;
    return h;
}

// Two directions lying on the surface, the same for every pixel that shares this normal.
void GetHullDamageSurfaceFrame(float3 normalOS, out float3 tangent, out float3 bitangent)
{
    float3 axis = abs(normalOS.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
    tangent = normalize(cross(axis, normalOS));
    bitangent = cross(normalOS, tangent);
}

// Distance from a mark's centre in outline units: 1 is the outline. The outline sits between a circle and a star:
// 6-10 soft lobes of random length (0.6-1 of the radius); starStrength sets how far the dips between them pull in.
// Marks below starMinSize stay round. distance: plain distance in mark radii. planar: offset from the centre along
// the surface. tangent/bitangent: the pixel's surface frame. size01: 0 = smallest mark.
// The outline never reaches past the plain radius, so the result is never smaller than distance.
float GetHullDamageShapeDistance(float distance, float3 planar, float3 tangent, float3 bitangent, float size01,
    float4 random)
{
    float spikes = _HullDamageCore.y * smoothstep(_HullDamageCore.z - 0.15, _HullDamageCore.z, size01);
    // Past 1 + the largest edge breakup (0.5, HullDamageSettings) no soot is left, so the shape is not needed.
    if (spikes <= 0.0 || distance >= 1.5)
        return distance;

    float rays = 6.0 + floor(frac(random.w * 7.31) * 5.0);
    float turns = (atan2(dot(planar, bitangent), dot(planar, tangent)) / HULL_DAMAGE_TWO_PI + random.x) * rays;
    float across = frac(turns + 0.5) - 0.5;                               // -0.5..0.5 across one lobe
    float rayLength = lerp(0.6, 1.0, frac(sin(floor(turns + 0.5) * 12.9898 + random.y * 78.233) * 43758.5453));
    float onRay = pow(saturate(1.0 - abs(across) * 2.0), 1.5);           // 1 on the lobe, 0 halfway to the next
    float outline = lerp(1.0, lerp(0.5, rayLength, onRay), spikes);
    return distance / outline;
}

// The two nearest active marks: x/z = outline distance (1 = outline; large when none), y/w = each mark's own
// random number (0..1) for its opacity. Two, so overlapping marks blend instead of meeting at a hard seam.
// nearestPatch: xyz = offset from the nearest mark's burnt-patch centre along the surface, in that mark's radii.
// The patch centre sits at a random spot inside the mark.
// A mark lies on the surface: its spot must be within one radius of the surface, and the distance is measured
// along the surface, so a spot just under the plating still leaves a full-size mark.
// A mark reaches at most one cell from its spot (clamped in HullDamageService), so the 3x3x3 cells around
// this point are enough.
// Cost: cells whose every possible spot lies farther than the largest radius from the surface plane are skipped
// before hashing (on flat plating that is most of the layer above and below); the star shape is only evaluated
// for marks that can still be one of the two nearest.
float4 GetNearestHullDamageMarks(float3 cellPosition, float3 normalOS, half level, out float3 nearestPatch)
{
    float activeShare = level * _HullDamageParams.w;
    float maxRadius = _HullDamageParams.z;
    // A spot lies anywhere in its unit cell, so it is at most sqrt(3)/2 from the cell centre.
    float planeReach = 0.8660254 + maxRadius;
    float3 tangent, bitangent;
    GetHullDamageSurfaceFrame(normalOS, tangent, bitangent);
    int3 firstCell = (int3)floor(cellPosition) - 1;
    float4 nearest = float4(1e4, 0.0, 1e4, 0.0);
    nearestPatch = float3(1e4, 0.0, 0.0);
    for (int z = 0; z < 3; z++)
    for (int y = 0; y < 3; y++)
    for (int x = 0; x < 3; x++)
    {
        int3 cell = firstCell + int3(x, y, z);
        if (abs(dot(float3(cell) + 0.5 - cellPosition, normalOS)) >= planeReach)
            continue;
        // The share of damage at which this cell's mark appears; cheap hash first, full hash only if it is on.
        float activation = (float)HullDamageActivationHash(cell) * HULL_DAMAGE_UINT_TO_UNIT;
        if (activation >= activeShare)
            continue;
        float4 random = float4((float3)HullDamageHash(cell).xyz * HULL_DAMAGE_UINT_TO_UNIT, activation);
        float size01 = frac(random.w * 61.7);
        float radius = maxRadius * lerp(_HullDamageSurface.z, 1.0, size01);
        float3 offset = cellPosition - (float3(cell) + random.xyz);
        float depth = dot(offset, normalOS);
        if (abs(depth) >= radius)
            continue;
        float3 planar = offset - depth * normalOS;
        float distance = length(planar) / radius;
        if (distance >= nearest.z)
            continue;
        distance = GetHullDamageShapeDistance(distance, planar, tangent, bitangent, size01, random);
        float2 found = float2(distance, frac(random.w * 113.9));
        if (distance < nearest.x)
        {
            nearest = float4(found, nearest.xy);
            float3 patchShift = (frac(random.yzx * 17.13) - 0.5) * (2.0 * _HullDamageBurnt.x * radius);
            patchShift -= dot(patchShift, normalOS) * normalOS;
            nearestPatch = (planar - patchShift) / radius;
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

// Smooth 3D value noise, 0..1. Continuous everywhere, so outlines and patches have no blocks or tiling.
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

// Noise that shapes the burnt patches: fragmented, uneven pieces. cellPosition in mark cells.
float GetHullDamagePatchNoise(float3 cellPosition)
{
    float3 p = cellPosition * _HullDamageParams.x;
    return 0.6 * HullDamageValueNoise(p * 4.5 + 3.1) + 0.4 * HullDamageValueNoise(p * 10.3 + 9.7);
}

// How much the patch centre pulls the field up: 0.45 at the centre, -0.6 beyond the patch radius, so no piece
// appears outside it. patchDistance in mark radii.
float GetHullDamagePatchBias(float patchDistance)
{
    float inner = 1.0 - smoothstep(0.0, _HullDamageCore.w, patchDistance);
    return -0.4 + 0.45 * inner - 0.6 * (1.0 - inner);
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

void ApplyHullDamageSoot(inout SurfaceData surface, half soot)
{
    surface.albedo = lerp(surface.albedo, _HullDamageSootColor.rgb, soot);
    // Soot kills the metal sheen too; otherwise reflections turn a black mark grey.
    surface.metallic *= 1.0h - soot;
    surface.smoothness = lerp(surface.smoothness, 0.1h, soot);   // soot is matte
    surface.occlusion *= 1.0h - 0.5h * soot;
    // Running lights under soot dim with it.
    surface.emission *= 1.0h - soot;
}

// level: 0 = intact (skipped), 1 = destroyed.
void ApplyHullDamage(inout SurfaceData surface, float3 positionDamage, half level, half3 normalWS, half4 tangentWS)
{
    // How much hull one pixel covers, in mark cells. Derivatives first: they are only valid outside the branches.
    float footprint = max(length(ddx(positionDamage)), length(ddy(positionDamage)))
        * (_HullDamageParams.y / _HullDamageMarkScale);

    // One user value per draw, so every pixel of the draw takes the same side.
    UNITY_BRANCH
    if (level <= 0.0h)
    {
        return;
    }

    // Far away the marks shrink below a pixel: they blend into an even soot (the average of what the marks would
    // cover) between 2 and 1 pixels across the largest mark, and past that the mark search is skipped.
    float largestRadius = _HullDamageParams.z;
    half farBlend = smoothstep(largestRadius, 2.0 * largestRadius, footprint);
    half farSoot = level * _HullDamageBurnt.w * saturate(_HullDamageOpacityScale);
    UNITY_BRANCH
    if (farBlend >= 1.0h)
    {
        ApplyHullDamageSoot(surface, farSoot);
        return;
    }

    // The material's mark scale grows marks and their spacing together, so marks stay within the 27-cell search.
    float3 cellPosition = positionDamage * (_HullDamageParams.y / _HullDamageMarkScale);
    float3 normalOS = HullDamageWorldToPatternDir(normalWS);
    float3 patch;
    float4 marks = GetNearestHullDamageMarks(cellPosition, normalOS, level, patch);
    // Most of the hull is far from any mark: skip the rest there.
    UNITY_BRANCH
    if (marks.x >= 1.0 + _HullDamageSurface.y)
    {
        ApplyHullDamageSoot(surface, farSoot * farBlend);
        return;
    }

    // Detail (frayed outline, burnt patch) only where a mark is big enough on screen to show it: fades out between
    // 2x and 1x the minimum pixel size across the largest mark radius. Far-away ships get plain soot.
    half detail = 1.0h - smoothstep(largestRadius / (2.0 * _HullDamageBurnt.z), largestRadius / _HullDamageBurnt.z,
        footprint);
    bool fullQuality = _HullDamageQuality >= HULL_DAMAGE_FULL_QUALITY_LEVEL;

    // Smooth noise waves the outline into an irregular stain (two octaves at full quality).
    float fray = 0.0;
    UNITY_BRANCH
    if (detail > 0.0h)
    {
        float3 noisePosition = cellPosition * _HullDamageParams.x;
        float noise = HullDamageValueNoise(noisePosition);
        if (fullQuality)
            noise = 0.65 * noise + 0.35 * HullDamageValueNoise(noisePosition * 2.3 + 11.7);
        fray = (noise - 0.5) * 2.0 * _HullDamageSurface.y * detail;
    }
    // Overlapping marks stack like two layers of soot.
    half soot = 1.0h - (1.0h - GetHullDamageSoot(marks.x + fray, marks.y))
        * (1.0h - GetHullDamageSoot(marks.z + fray, marks.w));
    ApplyHullDamageSoot(surface, lerp(soot, farSoot, farBlend));

    UNITY_BRANCH
    if (detail <= 0.0h)
    {
        return;
    }

    // Burnt patch: irregular burnt metal around a random spot inside the nearest mark, sunken, with a glowing rim.
    // The noise decides the shape; nearness to the patch centre only biases it, so no patch repeats another or
    // follows the outline.
    float patchDistance = length(patch);
    float patchNoise = GetHullDamagePatchNoise(cellPosition);
    float field = patchNoise + GetHullDamagePatchBias(patchDistance);
    half rimWidth = _HullDamageEmberColor.a;
    UNITY_BRANCH
    if (field < HULL_DAMAGE_PATCH_THRESHOLD - rimWidth)
    {
        return;
    }

    half burnt = smoothstep(HULL_DAMAGE_PATCH_THRESHOLD, HULL_DAMAGE_PATCH_THRESHOLD + 0.03, field) * detail;
    half rim = smoothstep(HULL_DAMAGE_PATCH_THRESHOLD - rimWidth, HULL_DAMAGE_PATCH_THRESHOLD, field)
        * (1.0h - smoothstep(HULL_DAMAGE_PATCH_THRESHOLD + 0.01, HULL_DAMAGE_PATCH_THRESHOLD + rimWidth, field)) * detail;
    // Deeper inside the patch: darker metal, and a shadow along the inside of the wall.
    half deep = smoothstep(HULL_DAMAGE_PATCH_THRESHOLD, HULL_DAMAGE_PATCH_THRESHOLD + 0.2, field);
    surface.albedo = lerp(surface.albedo, _HullDamageBurntColor.rgb * lerp(1.0h, 0.55h, deep), burnt);
    surface.occlusion *= 1.0h - 0.6h * burnt * (1.0h - deep);
    surface.emission += _HullDamageEmberColor.rgb * (rim + 0.05h * burnt);

    // Lower quality levels keep the patches flat.
    UNITY_BRANCH
    if (!fullQuality)
    {
        return;
    }

    // Sunken look: on the patch wall (where the field crosses the threshold) tilt the normal toward the inside,
    // i.e. along the field's slope over the surface. The slope comes from finite differences, not ddx: this runs
    // inside a per-pixel branch.
    float3 tangent, bitangent;
    GetHullDamageSurfaceFrame(normalOS, tangent, bitangent);
    float3 patchTangent = tangent * (HULL_DAMAGE_SLOPE_STEP / _HullDamageParams.z);
    float3 patchBitangent = bitangent * (HULL_DAMAGE_SLOPE_STEP / _HullDamageParams.z);
    float fieldT = GetHullDamagePatchNoise(cellPosition + tangent * HULL_DAMAGE_SLOPE_STEP)
        + GetHullDamagePatchBias(length(patch + patchTangent));
    float fieldB = GetHullDamagePatchNoise(cellPosition + bitangent * HULL_DAMAGE_SLOPE_STEP)
        + GetHullDamagePatchBias(length(patch + patchBitangent));
    float3 slope = tangent * (fieldT - field) + bitangent * (fieldB - field);
    float wallU = saturate((field - (HULL_DAMAGE_PATCH_THRESHOLD - 0.02)) / 0.14);
    float wall = 4.0 * wallU * (1.0 - wallU);
    float3 tiltedOS = normalize(normalOS + slope / max(length(slope), 1e-5) * (_HullDamageBurnt.y * wall * detail));
    float3 tiltedWS = HullDamagePatternToWorldDir(tiltedOS);
    half3 bitangentWS = tangentWS.w * cross(normalWS, tangentWS.xyz);
    half2 tiltTS = half2(dot(tiltedWS, tangentWS.xyz), dot(tiltedWS, bitangentWS));
    surface.normalTS = normalize(half3(surface.normalTS.xy + tiltTS, surface.normalTS.z));
}

#endif // SHIP_HULL_DAMAGE_INCLUDED
