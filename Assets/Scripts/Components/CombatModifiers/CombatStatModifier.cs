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
        [SerializeField] private float shotIntervalMultiplier;

        public float DamageMultiplier => damageMultiplier;
        public float FireDelayMultiplier => fireDelayMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float ShieldRegenMultiplier => shieldRegenMultiplier;
        public float DamageTakenMultiplier => damageTakenMultiplier;
        // Older serialized modifiers omit this field.
        public float ShotIntervalMultiplier => shotIntervalMultiplier == 0f ? 1f : shotIntervalMultiplier;

        public CombatStatModifier(float damageMultiplier, float fireDelayMultiplier,
            float speedMultiplier, float shieldRegenMultiplier, float damageTakenMultiplier, float shotIntervalMultiplier = 1f)
        {
            this.damageMultiplier = damageMultiplier;
            this.fireDelayMultiplier = fireDelayMultiplier;
            this.speedMultiplier = speedMultiplier;
            this.shieldRegenMultiplier = shieldRegenMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
            this.shotIntervalMultiplier = shotIntervalMultiplier;
        }

        public bool Equals(CombatStatModifier other) =>
            damageMultiplier.Equals(other.damageMultiplier) &&
            fireDelayMultiplier.Equals(other.fireDelayMultiplier) &&
            speedMultiplier.Equals(other.speedMultiplier) &&
            shieldRegenMultiplier.Equals(other.shieldRegenMultiplier) &&
            damageTakenMultiplier.Equals(other.damageTakenMultiplier) &&
            ShotIntervalMultiplier.Equals(other.ShotIntervalMultiplier);

        public override bool Equals(object obj) => obj is CombatStatModifier other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(damageMultiplier,
            fireDelayMultiplier, speedMultiplier, shieldRegenMultiplier, damageTakenMultiplier, ShotIntervalMultiplier);
    }
}
