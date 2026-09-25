using System;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Combat
{
    public sealed class CombatModifiers
    {
        private readonly List<CombatStatModifier> _active = new List<CombatStatModifier>();

        public event Action Changed;

        public bool IsIonDisabled { get; private set; }

        public void SetIonDisabled(bool disabled)
        {
            IsIonDisabled = disabled;
            Changed?.Invoke();
        }

        public float DamageMultiplier { get; private set; } = 1f;
        public float FireDelayMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float ShieldRegenMultiplier { get; private set; } = 1f;
        public float DamageTakenMultiplier { get; private set; } = 1f;

        public void Add(CombatStatModifier modifier)
        {
            _active.Add(modifier);
            Recalculate();
        }

        public void Remove(CombatStatModifier modifier)
        {
            if (!_active.Remove(modifier))
                throw new InvalidOperationException("Combat modifier was not active.");
            Recalculate();
        }

        private void Recalculate()
        {
            float damage = 1f;
            float fireDelay = 1f;
            float speed = 1f;
            float shieldRegen = 1f;
            float damageTaken = 1f;
            foreach (CombatStatModifier modifier in _active)
            {
                damage *= modifier.DamageMultiplier;
                fireDelay *= modifier.FireDelayMultiplier;
                speed *= modifier.SpeedMultiplier;
                shieldRegen *= modifier.ShieldRegenMultiplier;
                damageTaken *= modifier.DamageTakenMultiplier;
            }

            DamageMultiplier = damage;
            FireDelayMultiplier = fireDelay;
            SpeedMultiplier = speed;
            ShieldRegenMultiplier = shieldRegen;
            DamageTakenMultiplier = damageTaken;
            Changed?.Invoke();
        }
    }
}
