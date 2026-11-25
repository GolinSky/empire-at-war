#ifndef EMPIRE_SHOCKWAVE_NOISE_INCLUDED
#define EMPIRE_SHOCKWAVE_NOISE_INCLUDED

float ShockwaveHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1) * 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Smooth value noise in -1..1.
float Noise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n = lerp(lerp(lerp(ShockwaveHash(i), ShockwaveHash(i + float3(1, 0, 0)), f.x),
                        lerp(ShockwaveHash(i + float3(0, 1, 0)), ShockwaveHash(i + float3(1, 1, 0)), f.x), f.y),
                   lerp(lerp(ShockwaveHash(i + float3(0, 0, 1)), ShockwaveHash(i + float3(1, 0, 1)), f.x),
                        lerp(ShockwaveHash(i + float3(0, 1, 1)), ShockwaveHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
    return n * 2.0 - 1.0;
}

// Three octaves of Noise, in 0..1.
float Fbm(float3 x)
{
    float sum = Noise(x) * 0.5 + Noise(x * 2.03 + 11.7) * 0.25 + Noise(x * 4.01 + 23.1) * 0.125;
    return sum / 0.875 * 0.5 + 0.5;
}

#endif
