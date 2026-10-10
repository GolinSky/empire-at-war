using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    /// <summary>
    /// Shields protect everything until they drop (unless the damage type pierces them).
    /// Hull hits land on a hardpoint: the hardpoint loses the damage, the hull loses damage × hardpoint multiplier.
    /// The ship dies only when the hull reaches zero.
    /// </summary>
    [Serializable]
    public class HealthModel : Model
    {
        private readonly IHealthData _data;

        private readonly DamageMatrixData _damageMatrix;
        private readonly CombatModifiers _modifiers;
        private float _hullMultiplier;
        private float _shieldsMultiplier;
        private int _level = 1;
        private float _levelHullScale = 1f;
        private float _levelShieldsScale = 1f;
        private float _levelShieldRegenerateScale = 1f;

        public event Action OnValueChanged;

        public event Action OnDestroy;

        public ShipClass ShipClass => _data.ShipClass;
        public float Hull { get; private set; }
        public float MaxHull => _data.Hull * _hullMultiplier * _levelHullScale;
        public float HullPercentage => MaxHull <= 0f ? 0f : Hull / MaxHull;
        public float Shields { get; private set; }
        public float ShieldPercentage => MaxShields <= 0f ? 0f : Shields / MaxShields;
        public float MaxShields => _data.Shields * _shieldsMultiplier * _levelShieldsScale;
        public float ShieldRegenerateValue => _data.ShieldRegenerateValue * _levelShieldRegenerateScale;
        public float ShieldRegenerateDelay => _data.ShieldRegenerateDelay;
        public HardPointModel[] HardPointModels { get; private set; } = Array.Empty<HardPointModel>();
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0f;
        public bool IsLostShieldGenerator { get; private set; }
        public bool HasUnits => !IsDestroyed;
        public bool HasLiveHardPoints => HardPointModels.Any(hardPoint => !hardPoint.IsDestroyed);

        public HealthModel(IHealthData data, DamageMatrixData damageMatrix, CombatModifiers modifiers)
        {
            _data = data;
            _damageMatrix = damageMatrix;
            _modifiers = modifiers;
            _hullMultiplier = modifiers.HullMultiplier;
            _shieldsMultiplier = modifiers.ShieldsMultiplier;
            Hull = MaxHull;
            Shields = MaxShields;
        }

        public void RefreshStatModifiers()
        {
            if (_hullMultiplier == _modifiers.HullMultiplier &&
                _shieldsMultiplier == _modifiers.ShieldsMultiplier) return;
            Hull *= _modifiers.HullMultiplier / _hullMultiplier;
            Shields *= _modifiers.ShieldsMultiplier / _shieldsMultiplier;
            _hullMultiplier = _modifiers.HullMultiplier;
            _shieldsMultiplier = _modifiers.ShieldsMultiplier;
            OnValueChanged?.Invoke();
        }

        public void InitializeHardPoints(IReadOnlyList<HardPointModel> hardPointModels)
        {
            HardPointModels = hardPointModels.ToArray();
            foreach (HardPointModel hardPoint in HardPointModels)
            {
                if (hardPoint.UnlockLevel > _level)
                {
                    hardPoint.Uninstall();
                    continue;
                }

                HardPointHealth health = GetHardPointHealth(hardPoint.HardPointType);
                hardPoint.SetHealth(health.Health, health.HullDamageMultiplier);
            }
        }

        /// <summary>
        /// Raises the unit to an upgrade level: hull, shields and shield regeneration take the level's scales
        /// (current hull and shields keep their percentage), hardpoints unlocked by the level are installed
        /// and every installed hardpoint, the shield generator included, is restored to full health.
        /// </summary>
        public void Upgrade(int level, float hullScale, float shieldsScale, float shieldRegenerateScale)
        {
            if (IsDestroyed)
            {
                return;
            }

            float hullPercentage = HullPercentage;
            float shieldPercentage = ShieldPercentage;
            _level = level;
            _levelHullScale = hullScale;
            _levelShieldsScale = shieldsScale;
            _levelShieldRegenerateScale = shieldRegenerateScale;
            Hull = MaxHull * hullPercentage;
            Shields = MaxShields * shieldPercentage;
            IsLostShieldGenerator = false;
            InitializeHardPoints(HardPointModels);
            OnValueChanged?.Invoke();
        }

        public void ApplyDamage(float damage, DamageType damageType, int hardPointId)
        {
            if (IsDestroyed)
            {
                return;
            }

            damage *= _modifiers.DamageTakenMultiplier;
            if (AbsorbsDamage(damageType))
            {
                Shields = Math.Max(0f, Shields - damage * _damageMatrix.GetShieldMultiplier(damageType));
            }
            else
            {
                float hullDamage = damage * _damageMatrix.GetDamageMultiplier(damageType, ShipClass);
                if (HardPointModels.Length == 0)
                {
                    DamageHull(hullDamage);
                }
                else
                {
                    DamageHardPoint(HardPointModels[hardPointId], hullDamage);
                }
            }

            OnValueChanged?.Invoke();
        }

        /// <summary>Destroys the unit at once, ignoring shields and hardpoints (cheats and debugging).</summary>
        public void Kill()
        {
            if (IsDestroyed)
            {
                return;
            }

            Shields = 0f;
            Hull = 0f;
            IsDestroyed = true;
            OnValueChanged?.Invoke();
            OnDestroy?.Invoke();
        }

        public void RegenerateShields(float value)
        {
            Shields = Math.Min(MaxShields, Shields + value);
            OnValueChanged?.Invoke();
        }

        public bool AbsorbsDamage(DamageType damageType)
        {
            return !IsDestroyed && HasShields && !IsLostShieldGenerator &&
                   !_damageMatrix.IsShieldPiercing(damageType);
        }

        private void DamageHardPoint(HardPointModel hardPoint, float damage)
        {
            if (!hardPoint.IsDestroyed)
            {
                hardPoint.ApplyDamage(damage);
                if (hardPoint.IsDestroyed && hardPoint.HardPointType == HardPointType.ShieldGenerator &&
                    HardPointModels.Where(point => point.HardPointType == HardPointType.ShieldGenerator)
                        .All(point => point.IsDestroyed))
                {
                    IsLostShieldGenerator = true;
                    Shields = 0f;
                }
            }

            DamageHull(damage * hardPoint.HullDamageMultiplier);
        }

        private void DamageHull(float damage)
        {
            Hull = Math.Max(0f, Hull - damage);
            if (Hull <= 0f)
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
        }

        private HardPointHealth GetHardPointHealth(HardPointType hardPointType)
        {
            foreach (HardPointHealth health in _data.HardPointHealth)
            {
                if (health.HardPointType == hardPointType)
                {
                    return health;
                }
            }

            throw new InvalidOperationException(
                $"Health data for {_data.ShipClass} has no {nameof(HardPointHealth)} entry for {hardPointType}.");
        }
    }
}
