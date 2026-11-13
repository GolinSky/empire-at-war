using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(menuName = "Empire At War/Map/Planet Decorations")]
    public sealed class PlanetDecorationSet : ScriptableObject
    {
        [SerializeField] private PlanetDecorationEntry[] entries = Array.Empty<PlanetDecorationEntry>();
        [field: SerializeField, Range(0.1f, 1f)] public float PlacementBoundsFraction { get; private set; } = 1f;
        public IReadOnlyList<PlanetDecorationEntry> Entries => entries;
    }
}
