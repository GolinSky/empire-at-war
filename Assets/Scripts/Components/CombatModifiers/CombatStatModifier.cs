using System;
using UnityEngine;

namespace EmpireAtWar.Components.Combat
{
    [Serializable]
    public struct CombatStatModifier : IEquatable<CombatStatModifier>
    {
        [SerializeField] private float damageMultiplier;
        [SerializeField] private float fireDelayMultiplier;
        [SerializeField] private float speedMultiplier;
        [SerializeField] private float shieldRegenMultiplier;
        [SerializeField] private float damageTakenMultiplier;

        public float DamageMultiplier => damageMultiplier;
        public float FireDelayMultiplier => fireDelayMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float ShieldRegenMultiplier => shieldRegenMultiplier;
        public float DamageTakenMultiplier => damageTakenMultiplier;

        public CombatStatModifier(float damageMultiplier, float fireDelayMultiplier,
            float speedMultiplier, float shieldRegenMultiplier, float damageTakenMultiplier)
        {
            this.damageMultiplier = damageMultiplier;
            this.fireDelayMultiplier = fireDelayMultiplier;
            this.speedMultiplier = speedMultiplier;
            this.shieldRegenMultiplier = shieldRegenMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
        }

        public bool Equals(CombatStatModifier other) =>
            damageMultiplier.Equals(other.damageMultiplier) &&
            fireDelayMultiplier.Equals(other.fireDelayMultiplier) &&
            speedMultiplier.Equals(other.speedMultiplier) &&
            shieldRegenMultiplier.Equals(other.shieldRegenMultiplier) &&
            damageTakenMultiplier.Equals(other.damageTakenMultiplier);

        public override bool Equals(object obj) => obj is CombatStatModifier other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(damageMultiplier,
            fireDelayMultiplier, speedMultiplier, shieldRegenMultiplier, damageTakenMultiplier);
    }
}
