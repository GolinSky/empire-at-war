using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    [Serializable]
    public struct HardPointHealth
    {
        [SerializeField] private HardPointType hardPointType;
        [SerializeField] private float health;
        [Tooltip("Share of the damage a hit on this hardpoint also deals to the hull.")]
        [SerializeField] private float hullDamageMultiplier;

        public HardPointType HardPointType => hardPointType;
        public float Health => health;
        public float HullDamageMultiplier => hullDamageMultiplier;
    }
}
