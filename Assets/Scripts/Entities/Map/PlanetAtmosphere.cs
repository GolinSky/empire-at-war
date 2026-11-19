using System;
using EmpireAtWar.Entities.Planet;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class PlanetAtmosphere
    {
        [field: SerializeField] public PlanetBiome Biome { get; private set; }

        [field: Header("Sun")]
        [field: SerializeField] public Color SunColor { get; private set; } = Color.white;
        [field: SerializeField, Min(0f)] public float SunIntensity { get; private set; } = 1f;

        [field: Header("Ambient (Trilight)")]
        [field: SerializeField] public Color AmbientSky { get; private set; } = Color.gray;
        [field: SerializeField] public Color AmbientEquator { get; private set; } = Color.gray;
        [field: SerializeField] public Color AmbientGround { get; private set; } = Color.gray;

        [field: Header("Post-processing")]
        [field: SerializeField, Tooltip("Sparse profile blended over the base battle profile.")]
        public VolumeProfile Grade { get; private set; }
    }
}
