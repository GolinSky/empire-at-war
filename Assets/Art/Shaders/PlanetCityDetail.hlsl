#ifndef PLANET_CITY_DETAIL_INCLUDED
#define PLANET_CITY_DETAIL_INCLUDED

TEXTURE2D(_CityDetailMap);
SAMPLER(sampler_CityDetailMap);
float _CityDetailTiling;
half _CityDetailStrength;
half4 _CityEmissionColor;

void InitializePlanetSurfaceData(float2 uv, float3 positionWS, out SurfaceData surfaceData)
{
    InitializeStandardLitSurfaceData(uv, surfaceData);

#if defined(_PLANET_CITY_DETAIL)
    // Use the surface position because the built-in sphere's UVs distort near its poles.
    // Undo GPU rotation so both texture layers remain attached to the rotating surface.
    float3 direction = normalize(TransformWorldToObject(positionWS));
    float sine, cosine;
    sincos(-radians(_PlanetRotation.w * (_Time.y - _PlanetRotationStartTime)), sine, cosine);
    float3 axis = _PlanetRotation.xyz;
    direction = direction * cosine + cross(axis, direction) * sine
        + axis * dot(axis, direction) * (1.0 - cosine);

    float2 sphereUV = float2(atan2(direction.z, direction.x) / TWO_PI + 0.5,
        asin(clamp(direction.y, -1.0, 1.0)) / PI + 0.5);
    // Longitude wraps at the seam; explicit gradients prevent a false coarse mip there.
    float2 gradientX = ddx(sphereUV);
    float2 gradientY = ddy(sphereUV);
    gradientX.x -= round(gradientX.x);
    gradientY.x -= round(gradientY.x);
    sphereUV = sphereUV * _BaseMap_ST.xy + _BaseMap_ST.zw;
    gradientX *= _BaseMap_ST.xy;
    gradientY *= _BaseMap_ST.xy;
    surfaceData.albedo = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, sphereUV,
        gradientX, gradientY).rgb * _BaseColor.rgb;
    surfaceData.emission = SAMPLE_TEXTURE2D_GRAD(_EmissionMap, sampler_EmissionMap, sphereUV,
        gradientX, gradientY).rgb * _EmissionColor.rgb;

    float3 weights = pow(abs(direction), 8.0);
    weights /= weights.x + weights.y + weights.z;
    float3 position = direction * (0.5 * _CityDetailTiling);

    half4 detail = SAMPLE_TEXTURE2D(_CityDetailMap, sampler_CityDetailMap, position.zy) * weights.x;
    detail += SAMPLE_TEXTURE2D(_CityDetailMap, sampler_CityDetailMap, position.xz) * weights.y;
    detail += SAMPLE_TEXTURE2D(_CityDetailMap, sampler_CityDetailMap, position.xy) * weights.z;

    surfaceData.albedo *= lerp(1.0h, detail.rgb * 2.0h, _CityDetailStrength);
    surfaceData.emission += detail.a * _CityEmissionColor.rgb;
#endif
}

#endif
