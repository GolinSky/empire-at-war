using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, Min(0f)] private float heavyCruiser = 117f;
        [SerializeField, Min(0f)] private float capital = 216f;
        [FormerlySerializedAs("heavyCapital"), SerializeField, Min(0f)] private float battleCruiser = 400f;
        [FormerlySerializedAs("executor"), SerializeField, Min(0f)] private float dreadnought = 1111f;

        public float GetRegenerateValue(ShieldRegenTier tier)
        {
            switch (tier)
            {
                case ShieldRegenTier.Corvette: return corvette;
                case ShieldRegenTier.Frigate: return frigate;
                case ShieldRegenTier.Cruiser: return cruiser;
                case ShieldRegenTier.HeavyCruiser: return heavyCruiser;
                case ShieldRegenTier.Capital: return capital;
                case ShieldRegenTier.BattleCruiser: return battleCruiser;
                case ShieldRegenTier.Dreadnought: return dreadnought;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }
}
