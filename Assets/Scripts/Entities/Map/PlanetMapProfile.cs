using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(fileName = nameof(PlanetMapProfile), menuName = "Data/Planet Map Profiles")]
    public sealed class PlanetMapProfile : ScriptableObject
    {
        [SerializeField] private PlanetMapProfileEntry[] entries = Array.Empty<PlanetMapProfileEntry>();
        public IReadOnlyList<PlanetMapProfileEntry> Entries => entries;

        public PlanetMapProfileEntry Get(PlanetType planet)
        {
            PlanetMapProfileEntry result = null;
            foreach (PlanetMapProfileEntry entry in entries)
            {
                if (entry.Planet != planet) continue;
                if (result != null) throw new InvalidOperationException($"Duplicate map profile for {planet}.");
                result = entry;
            }
            if (result == null || ReferenceEquals(result.Archetype, null))
                throw new InvalidOperationException($"A map archetype profile is required for {planet}.");
            return result;
        }
    }
}
