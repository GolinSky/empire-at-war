// ShipWreckDeformation.hlsl
// -----------------------------------------------------------------------------
// Break-apart and dissolve math shared by every pass of "EmpireAtWar/Ship Wreck".
//
// The wreck renders the ship's own meshes. The shader cuts them across the ship's long axis into 2 or 3 parts:
//   part = how many cuts lie before the vertex along _WreckAxis (0, 1 or 2), all in world space.
// A triangle that crosses a cut gets a fractional part id after interpolation; ClipWreck discards
// those pixels, which leaves a torn edge instead of triangles stretched across the gap.
// Each part then separates along the axis, tilts, rolls and sinks. Everything is a pure function
// of time: nothing is updated from C# after the spawn.
// -----------------------------------------------------------------------------
#ifndef SHIP_WRECK_DEFORMATION_INCLUDED
#define SHIP_WRECK_DEFORMATION_INCLUDED

// Seconds since the wreck became visible. Negative while the explosion still hides the swap.
float GetWreckAge()
{
    return _Time.y - _WreckStartTime;
}

float GetObjectScale()
{
    return length(GetObjectToWorldMatrix()._m00_m10_m20);
}

// The cut frame is in world space, so every mesh of a multi-mesh ship breaks along the same lines
// and all of them can share one material instance.
float GetAxisPosition(float3 positionOS)
{
    return dot(TransformObjectToWorld(positionOS) - _WreckCenter.xyz, _WreckAxis.xyz);
}

// 0, 1 or 2: which part of the ship this position belongs to.
float GetWreckPart(float3 positionOS)
{
    float axisPosition = GetAxisPosition(positionOS);
    return step(_WreckCuts.x, axisPosition) + step(_WreckCuts.y, axisPosition);
}

// 0..1 random value per part, different for every wreck.
float GetPartRandom(float part)
{
    return frac(sin((part + 1.0) * 12.9898 + _WreckSeed * 78.233) * 43758.5453);
}

// Rodrigues' rotation of v around a unit axis.
float3 RotateAroundAxis(float3 v, float3 axis, float angle)
{
    float sine;
    float cosine;
    sincos(angle, sine, cosine);
    return v * cosine + cross(axis, v) * sine + axis * dot(axis, v) * (1.0 - cosine);
}

// Moves one vertex with its part: tilt and roll around the part's center, drift away from the
// ship's middle along the axis, and sink. Returns the part id for the fragment clip.
float ApplyWreckDeformation(inout float3 positionOS, inout float3 normalOS, inout float3 tangentOS)
{
    float part = GetWreckPart(positionOS);
    float age = max(0.0, GetWreckAge());
    float random = GetPartRandom(part);
    float3 axis = _WreckAxis.xyz;

    float partStart = part < 0.5 ? _WreckAxisRange.x : (part < 1.5 ? _WreckCuts.x : _WreckCuts.y);
    float partEnd = part < 0.5 ? _WreckCuts.x : (part < 1.5 && _WreckCuts.z > 2.5 ? _WreckCuts.y : _WreckAxisRange.y);
    float partMiddle = (partStart + partEnd) * 0.5;
    float3 pivot = _WreckCenter.xyz + axis * partMiddle;

    // Parts nose down/up away from the break (pitch) and roll a little around the ship axis.
    float3 up = float3(0.0, 1.0, 0.0);
    float3 pitchAxis = normalize(cross(axis, up) + float3(0.0001, 0.0, 0.0));
    // In a three-part wreck the middle part stays and drops; the end parts pull away from it.
    bool isMiddlePart = _WreckCuts.z > 2.5 && part > 0.5 && part < 1.5;
    float awayFromMiddle = isMiddlePart ? 0.0 : sign(partMiddle - (_WreckAxisRange.x + _WreckAxisRange.y) * 0.5);
    float sinkScale = isMiddlePart ? 1.8 : lerp(0.7, 1.3, random);
    float pitchSign = isMiddlePart ? (random < 0.5 ? -1.0 : 1.0) : awayFromMiddle;
    float pitch = radians(_WreckTiltSpeed) * age * lerp(0.5, 1.0, random) * pitchSign;
    float roll = radians(_WreckTiltSpeed) * age * (random - 0.5);

    float3 offset = TransformObjectToWorld(positionOS) - pivot;
    offset = RotateAroundAxis(offset, pitchAxis, pitch);
    offset = RotateAroundAxis(offset, axis, roll);
    float3 normalWS = RotateAroundAxis(RotateAroundAxis(TransformObjectToWorldNormal(normalOS), pitchAxis, pitch), axis, roll);
    float3 tangentWS = RotateAroundAxis(RotateAroundAxis(TransformObjectToWorldDir(tangentOS), pitchAxis, pitch), axis, roll);

    float3 drift = axis * awayFromMiddle * _WreckSeparationSpeed * age - up * _WreckSinkSpeed * age * sinkScale;
    positionOS = TransformWorldToObject(pivot + offset + drift);
    normalOS = TransformWorldToObjectNormal(normalWS);
    tangentOS = TransformWorldToObjectDir(tangentWS);
    return part;
}

float WreckHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float WreckValueNoise(float3 x)
{
    float3 cell = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(WreckHash(cell), WreckHash(cell + float3(1, 0, 0)), f.x),
             lerp(WreckHash(cell + float3(0, 1, 0)), WreckHash(cell + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(WreckHash(cell + float3(0, 0, 1)), WreckHash(cell + float3(1, 0, 1)), f.x),
             lerp(WreckHash(cell + float3(0, 1, 1)), WreckHash(cell + float3(1, 1, 1)), f.x), f.y),
        f.z);
}

// 0 = untouched, 1 = fully dissolved. Parts finish at slightly different moments, all before the lifetime ends.
float GetWreckDissolveProgress(float part)
{
    float dissolveStart = _WreckLifetime - _WreckDissolveDuration;
    float progress = (GetWreckAge() - dissolveStart) / max(_WreckDissolveDuration, 0.001);
    return saturate(progress * lerp(1.0, 1.3, GetPartRandom(part)));
}

// Returns how much of the burning dissolve edge covers this pixel (0..1). Discards the pixel before the
// wreck appears, on triangles torn by a cut (fractional part id), and where the dissolve ate the hull.
// restPositionOS is the undeformed position, so the noise pattern sticks to the hull.
half ClipWreck(float3 restPositionOS, float interpolatedPart)
{
    clip(GetWreckAge());
    clip(0.001 - abs(interpolatedPart - round(interpolatedPart)));

    float3 noisePosition = restPositionOS * GetObjectScale() * _WreckDissolveNoiseScale;
    float noise = WreckValueNoise(noisePosition) * 0.65 + WreckValueNoise(noisePosition * 2.7) * 0.35;
    float progress = GetWreckDissolveProgress(round(interpolatedPart));
    float remaining = noise - progress * 1.01;
    clip(remaining);
    return progress > 0.0 ? 1.0h - saturate(remaining / max(_WreckDissolveEdgeWidth, 0.001)) : 0.0h;
}

// Heat left by the explosion: 1 at the swap, cooling to 0 over the glow duration.
half GetWreckHeat()
{
    return saturate(1.0 - GetWreckAge() / max(_WreckGlowDuration, 0.001));
}

// 1 right at a cut, fading to 0 at the torn edge width (world units) from it.
half GetTornEdge(float3 restPositionOS)
{
    float axisPosition = GetAxisPosition(restPositionOS);
    float distanceToCut = min(abs(axisPosition - _WreckCuts.x), abs(axisPosition - _WreckCuts.y));
    return saturate(1.0 - distanceToCut / max(_WreckTornEdgeWidth, 0.001));
}

#endif // SHIP_WRECK_DEFORMATION_INCLUDED
