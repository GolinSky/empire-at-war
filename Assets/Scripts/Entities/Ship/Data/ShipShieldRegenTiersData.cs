using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Data
{
    /// <summary>
    /// Shields restored every regeneration tick, per tier. Regeneration runs under fire, so a tier cancels that much
    /// incoming shield damage per tick; a ship picks its tier (its class by default) in ShipData.
    /// </summary>
    [CreateAssetMenu(fileName = "ShipShieldRegenTiersData", menuName = "Data/ShipShieldRegenTiersData")]
    public class ShipShieldRegenTiersData : Mvc.Data
    {
        [SerializeField, Min(0f)] private float corvette = 5f;
        [SerializeField, Min(0f)] private float frigate = 25f;
        [SerializeField, Min(0f)] private float cruiser = 90f;
        [SerializeField, Min(0f)] private float capital = 180f;
        [SerializeField, Min(0f)] private float heavyCapital = 400f;
        [Tooltip("The Executor stays overpowered: its own tier keeps its former regeneration.")]
        [SerializeField, Min(0f)] private float executor = 1111.1111f;

        public float GetRegenerateValue(ShieldRegenTier tier)
        {
            switch (tier)
            {
                case ShieldRegenTier.Corvette: return corvette;
                case ShieldRegenTier.Frigate: return frigate;
                case ShieldRegenTier.Cruiser: return cruiser;
                case ShieldRegenTier.Capital: return capital;
                case ShieldRegenTier.HeavyCapital: return heavyCapital;
                case ShieldRegenTier.Executor: return executor;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }
}
