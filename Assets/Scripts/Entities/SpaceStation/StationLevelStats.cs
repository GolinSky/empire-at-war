using System;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    /// <summary>Durability of the station at one level, as multipliers of its level 1 health data.</summary>
    [Serializable]
    public struct StationLevelStats
    {
        [SerializeField, Min(0f)] private float hullMultiplier;
        [SerializeField, Min(0f)] private float shieldsMultiplier;
        [SerializeField, Min(0f)] private float shieldRegenerateMultiplier;

        public float HullMultiplier => hullMultiplier;
        public float ShieldsMultiplier => shieldsMultiplier;
        public float ShieldRegenerateMultiplier => shieldRegenerateMultiplier;
    }
}
