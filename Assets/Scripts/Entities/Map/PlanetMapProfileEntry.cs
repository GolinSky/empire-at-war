using System;
using EmpireAtWar.Entities.Planet;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class PlanetMapProfileEntry
    {
        [field: SerializeField] public PlanetType Planet { get; private set; }
        [field: SerializeField] public MapArchetype Archetype { get; private set; }
        [field: SerializeField] public Texture2D Preview { get; private set; }
        [field: SerializeField] public bool OverrideSeed { get; private set; }
        [field: SerializeField] public int PlanetSeed { get; private set; }
        [field: SerializeField, Min(0)] public int AdditionalRelayPairs { get; private set; }
        [field: SerializeField, Min(0)] public int AdditionalHazardPairs { get; private set; }

        public int GetSeed() => OverrideSeed ? PlanetSeed : MapPlanetSeed.FromPlanet(Planet);
    }
}
