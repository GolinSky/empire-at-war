using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(fileName = nameof(PlanetAtmosphereSettings), menuName = "Data/Planet Atmosphere Settings")]
    public sealed class PlanetAtmosphereSettings : ScriptableObject
    {
        [SerializeField] private PlanetAtmosphere[] entries = Array.Empty<PlanetAtmosphere>();
        public IReadOnlyList<PlanetAtmosphere> Entries => entries;

        public PlanetAtmosphere Get(PlanetBiome biome)
        {
            PlanetAtmosphere result = null;
            foreach (PlanetAtmosphere entry in entries)
            {
                if (entry.Biome != biome) continue;
                if (result != null) throw new InvalidOperationException($"Duplicate atmosphere for {biome}.");
                result = entry;
            }
            if (result == null) throw new InvalidOperationException($"An atmosphere is required for {biome}.");
            return result;
        }
    }
}
