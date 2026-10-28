using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Components.Combat
{
    public sealed class CombatModifiers
    {
        private const float UNMODIFIED_MULTIPLIER = 1f;
        private const float FLEET_COMMAND_DAMAGE_MULTIPLIER = 1.1f;
        private const float FLEET_COMMAND_SPEED_MULTIPLIER = 1.1f;
        private const float FLEET_COMMAND_DAMAGE_TAKEN_MULTIPLIER = 0.65f;
        private const float FLEET_COMMAND_HULL_MULTIPLIER = 1.2f;
        private const float FLEET_COMMAND_SHIELDS_MULTIPLIER = 1.1f;
        private const float FLEET_COMMAND_VISION_MULTIPLIER = 1.5f;
        private const float FIGHTER_COORDINATION_SPEED_MULTIPLIER = 1.15f;
        private const float FIGHTER_COORDINATION_ACCURACY_MULTIPLIER = 1.2f;
        private const float SENSOR_JAMMING_ACCURACY_MULTIPLIER = 0.8f;

        private readonly List<CombatModifierHandle> _active = new List<CombatModifierHandle>();
        private bool _fleetCommandActive;
        private IEntity _focusTarget;
        private float _focusDamageMultiplier = 1f;
        private float _projectileFireDelayMultiplier = 1f;
        private float _otherFireDelayMultiplier = 1f;
        private float _torpedoFireDelayMultiplier = 1f;
        private bool _mainBatteriesActive;
        private float _mainBatteryFireDelayMultiplier = 1f;

        public event Action Changed;

        private bool _superWeaponDisabled;
        private bool _isConstructing;
        private int _ionPulseDisables;
        private int _sensorJammers;
        private int _fighterCoordinations;
        private int _abilitySuppressions;
        private int _shieldSuppressions;
        public bool IsAbilitySuppressed => _abilitySuppressions > 0;
        public bool AbilitiesSuppressed => IsIonDisabled || IsAbilitySuppressed;
        public bool IsShieldSuppressed => _shieldSuppressions > 0;
        public float ShieldRecoveryDelay { get; private set; }
        public float AccuracyMultiplier =>
            (_sensorJammers > 0 ? SENSOR_JAMMING_ACCURACY_MULTIPLIER : UNMODIFIED_MULTIPLIER) *
            (_fighterCoordinations > 0 ? FIGHTER_COORDINATION_ACCURACY_MULTIPLIER : UNMODIFIED_MULTIPLIER);
        public bool IsIonDisabled => _superWeaponDisabled || _ionPulseDisables > 0;
        /// <summary>Effects that abilities or attackers remove later; fleet command is re-evaluated by position instead.</summary>
        public bool HasTemporaryEffects => _active.Count > 0 || _sensorJammers > 0 || _fighterCoordinations > 0 || IsIonDisabled || IsCloaked || _mainBatteriesActive ||
                                           _focusTarget != null || _projectileFireDelayMultiplier != 1f ||
                                           _otherFireDelayMultiplier != 1f || _torpedoFireDelayMultiplier != 1f;
        public bool IsCloaked { get; private set; }

        public void AddSensorJamming()
        {
            _sensorJammers++;
            Changed?.Invoke();
        }

        public void AddAbilitySuppression()
        {
            _abilitySuppressions++;
            Changed?.Invoke();
        }

        public void RemoveAbilitySuppression()
        {
            if (_abilitySuppressions == 0) throw new InvalidOperationException("No ability suppression is active.");
            _abilitySuppressions--;
            Changed?.Invoke();
        }

        public void AddShieldSuppression(float recoveryDelay = 2f)
        {
            ShieldRecoveryDelay = _shieldSuppressions == 0 ? recoveryDelay : Math.Max(ShieldRecoveryDelay, recoveryDelay);
            _shieldSuppressions++;
            Changed?.Invoke();
        }

        public void RemoveShieldSuppression()
        {
            if (_shieldSuppressions == 0) throw new InvalidOperationException("No shield suppression is active.");
            _shieldSuppressions--;
            Changed?.Invoke();
        }

        public void AddFighterCoordination()
        {
            _fighterCoordinations++;
            Recalculate();
        }

        public void RemoveFighterCoordination()
        {
            if (_fighterCoordinations == 0) throw new InvalidOperationException("No fighter coordination is active.");
            _fighterCoordinations--;
            Recalculate();
        }

        public void RemoveSensorJamming()
        {
            if (_sensorJammers == 0) throw new InvalidOperationException("No sensor jamming is active.");
            _sensorJammers--;
            Changed?.Invoke();
        }

        public void SetCloaked(bool cloaked)
        {
            IsCloaked = cloaked;
            if (Changed != null) Changed.Invoke();
        }

        public float DamageMultiplier { get; private set; } = 1f;
        public float FireDelayMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float ShieldRegenMultiplier { get; private set; } = 1f;
        public float DamageTakenMultiplier { get; private set; } = 1f;
        public float HullMultiplier => _fleetCommandActive ? FLEET_COMMAND_HULL_MULTIPLIER : UNMODIFIED_MULTIPLIER;
        public float ShieldsMultiplier => _fleetCommandActive ? FLEET_COMMAND_SHIELDS_MULTIPLIER : UNMODIFIED_MULTIPLIER;
        public float VisionMultiplier => _fleetCommandActive ? FLEET_COMMAND_VISION_MULTIPLIER : UNMODIFIED_MULTIPLIER;

        public float GetFullSalvoFireDelayMultiplier(bool interceptable) =>
            interceptable ? _projectileFireDelayMultiplier : _otherFireDelayMultiplier;

        public float GetTorpedoFireDelayMultiplier(bool torpedo) =>
            torpedo ? _torpedoFireDelayMultiplier : 1f;

        public void SetTorpedoSalvo(float fireDelayMultiplier)
        {
            _torpedoFireDelayMultiplier = fireDelayMultiplier;
            if (Changed != null) Changed.Invoke();
        }

        public bool CanFireWeapon(bool mainBattery) => !_isConstructing && (!_mainBatteriesActive || mainBattery);

        public void SetConstructionActive(bool constructing) => _isConstructing = constructing;

        public float GetMainBatteryFireDelayMultiplier(bool mainBattery) =>
            mainBattery ? _mainBatteryFireDelayMultiplier : 1f;

        public void SetMainBatteries(bool active, float fireDelayMultiplier)
        {
            _mainBatteriesActive = active;
            _mainBatteryFireDelayMultiplier = fireDelayMultiplier;
            if (Changed != null) Changed.Invoke();
        }

        public void SetFullSalvo(float projectileFireDelayMultiplier, float otherFireDelayMultiplier)
        {
            _projectileFireDelayMultiplier = projectileFireDelayMultiplier;
            _otherFireDelayMultiplier = otherFireDelayMultiplier;
            Changed?.Invoke();
        }

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

        public float GetDamageMultiplier(IEntity target) =>
            DamageMultiplier * (ReferenceEquals(target, _focusTarget) ? _focusDamageMultiplier : 1f);

        public void SetFocusFire(IEntity target, float damageMultiplier)
        {
            _focusTarget = target;
            _focusDamageMultiplier = damageMultiplier;
            Changed?.Invoke();
        }

        public void ClearFocusFire()
        {
            _focusTarget = null;
            _focusDamageMultiplier = 1f;
            Changed?.Invoke();
        }

        public bool IsFireDelayModified() => FireDelayMultiplier != UNMODIFIED_MULTIPLIER;

        public bool IsSpeedModified() => SpeedMultiplier != UNMODIFIED_MULTIPLIER;

        public bool IsDamageTakenModified() => DamageTakenMultiplier != UNMODIFIED_MULTIPLIER;

        /// <summary>Applies the modifier; pass the returned handle to <see cref="Remove"/> to end this application.</summary>
        public CombatModifierHandle Add(CombatStatModifier modifier)
        {
            var handle = new CombatModifierHandle(modifier);
            _active.Add(handle);
            Recalculate();
            return handle;
        }

        public void Remove(CombatModifierHandle handle)
        {
            if (!_active.Remove(handle))
                throw new InvalidOperationException("Combat modifier was not active.");
            Recalculate();
        }

        private void Recalculate()
        {
            float damage = _fleetCommandActive ? FLEET_COMMAND_DAMAGE_MULTIPLIER : UNMODIFIED_MULTIPLIER;
            float fireDelay = UNMODIFIED_MULTIPLIER;
            float speed = (_fleetCommandActive ? FLEET_COMMAND_SPEED_MULTIPLIER : UNMODIFIED_MULTIPLIER) *
                          (_fighterCoordinations > 0 ? FIGHTER_COORDINATION_SPEED_MULTIPLIER : UNMODIFIED_MULTIPLIER);
            float shieldRegen = UNMODIFIED_MULTIPLIER;
            // Defense is represented by incoming damage reduction in this combat system.
            float damageTaken = _fleetCommandActive ? FLEET_COMMAND_DAMAGE_TAKEN_MULTIPLIER : UNMODIFIED_MULTIPLIER;
            foreach (CombatModifierHandle handle in _active)
            {
                CombatStatModifier modifier = handle.Modifier;
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
