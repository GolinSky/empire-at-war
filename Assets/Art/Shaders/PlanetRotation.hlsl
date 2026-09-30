#ifndef PLANET_ROTATION_INCLUDED
#define PLANET_ROTATION_INCLUDED

// Per-renderer values set once by PlanetView: local axis, degrees/second, start time.
float4 _PlanetRotation;
float _PlanetRotationStartTime;

float3 RotatePlanetVector(float3 value)
{
    float angle = radians(_PlanetRotation.w * (_Time.y - _PlanetRotationStartTime));
    float sine, cosine;
    sincos(angle, sine, cosine);
    float3 axis = _PlanetRotation.xyz;
    return value * cosine + cross(axis, value) * sine
        + axis * dot(axis, value) * (1.0 - cosine);
}

void PlanetRotation_float(float3 Position, float3 Normal, float3 Tangent,
    out float3 RotatedPosition, out float3 RotatedNormal, out float3 RotatedTangent)
{
    RotatedPosition = RotatePlanetVector(Position);
    RotatedNormal = RotatePlanetVector(Normal);
    RotatedTangent = RotatePlanetVector(Tangent);
}

#endif
