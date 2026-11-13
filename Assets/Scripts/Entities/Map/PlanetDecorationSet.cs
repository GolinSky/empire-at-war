using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(menuName = "Empire At War/Map/Planet Decorations")]
    public sealed class PlanetDecorationSet : ScriptableObject
    {
        [SerializeField] private PlanetDecorationEntry[] entries = Array.Empty<PlanetDecorationEntry>();
        public IReadOnlyList<PlanetDecorationEntry> Entries => entries;
    }
}
