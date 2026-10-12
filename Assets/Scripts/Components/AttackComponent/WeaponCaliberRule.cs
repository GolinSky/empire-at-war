using System;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class WeaponCaliberRule
    {
        [SerializeField] private WeaponCaliber caliber;
        [Tooltip("Shot damage multiplier against the family base.")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [Tooltip("Range multiplier against the family base.")]
        [SerializeField, Min(0f)] private float rangeMultiplier = 1f;
        [Tooltip("Turret turn rate multiplier against the family base.")]
        [SerializeField, Min(0f)] private float turnRateMultiplier = 1f;
        [Tooltip("Extra damage multiplier per target class, on top of the damage matrix.")]
        [SerializeField] private ShipClassValues damageBias;
        [Tooltip("Extra accuracy multiplier per target class, on top of the damage matrix.")]
        [SerializeField] private ShipClassValues accuracyBias;

        public WeaponCaliber Caliber => caliber;
        public float DamageMultiplier => damageMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public float TurnRateMultiplier => turnRateMultiplier;
        public ShipClassValues DamageBias => damageBias;
        public ShipClassValues AccuracyBias => accuracyBias;
    }
}
