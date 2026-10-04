using System;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Combat
{
    public sealed class CombatModifiers
    {
        private const float UNMODIFIED_MULTIPLIER = 1f;

        private readonly List<CombatStatModifier> _active = new List<CombatStatModifier>();
        private bool _fleetCommandActive;

        public event Action Changed;

        private bool _superWeaponDisabled;
        private int _ionPulseDisables;
        public bool IsIonDisabled => _superWeaponDisabled || _ionPulseDisables > 0;

        public float DamageMultiplier { get; private set; } = 1f;
        public float FireDelayMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float ShieldRegenMultiplier { get; private set; } = 1f;
        public float DamageTakenMultiplier { get; private set; } = 1f;
        public float HullMultiplier => _fleetCommandActive ? 1.2f : 1f;
        public float ShieldsMultiplier => _fleetCommandActive ? 1.1f : 1f;
        public float VisionMultiplier => _fleetCommandActive ? 1.5f : 1f;

        public void SetFleetCommand(bool active)
        {
            if (_fleetCommandActive == active) return;
            _fleetCommandActive = active;
            Recalculate();
        }

        public void SetIonDisabled(bool disabled)
        {
            _superWeaponDisabled = disabled;
            Changed?.Invoke();
        }

        public void AddIonPulseDisable()
        {
            _ionPulseDisables++;
            Changed?.Invoke();
        }

        public void RemoveIonPulseDisable()
        {
            if (_ionPulseDisables == 0) throw new InvalidOperationException("No ion pulse disable is active.");
            _ionPulseDisables--;
            Changed?.Invoke();
        }

        public bool IsDamageDealtModified() => DamageMultiplier != UNMODIFIED_MULTIPLIER;

        public bool IsSpeedModified() => SpeedMultiplier != UNMODIFIED_MULTIPLIER;

        public bool IsDamageTakenModified() => DamageTakenMultiplier != UNMODIFIED_MULTIPLIER;

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
            float damage = _fleetCommandActive ? 1.1f : 1f;
            float fireDelay = 1f;
            float speed = _fleetCommandActive ? 1.1f : 1f;
            float shieldRegen = 1f;
            // Defense is represented by incoming damage reduction in this combat system.
            float damageTaken = _fleetCommandActive ? 0.65f : 1f;
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
