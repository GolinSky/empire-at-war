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
    public class HealthModel : PureModel
    {
        private readonly IHealthData _data;
        private readonly DamageMatrixData _damageMatrix;
        private readonly CombatModifiers _modifiers;

        public event Action OnValueChanged;
        public event Action OnDestroy;

        public ShipClass ShipClass => _data.ShipClass;
        public float Hull { get; private set; }
        public float HullPercentage => _data.Hull <= 0f ? 0f : Hull / _data.Hull;
        public float Shields { get; private set; }
        public float ShieldPercentage => _data.Shields <= 0f ? 0f : Shields / _data.Shields;
        public float MaxShields => _data.Shields;
        public float ShieldRegenerateValue => _data.ShieldRegenerateValue;
        public float ShieldRegenerateDelay => _data.ShieldRegenerateDelay;
        public HardPointModel[] HardPointModels { get; private set; } = Array.Empty<HardPointModel>();
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0f;
        public bool IsLostShieldGenerator { get; private set; }
        public bool HasUnits => !IsDestroyed && HardPointModels.Length > 0;
        public bool HasLiveHardPoints => HardPointModels.Any(hardPoint => !hardPoint.IsDestroyed);

        public HealthModel(IHealthData data, DamageMatrixData damageMatrix, CombatModifiers modifiers)
        {
            _data = data;
            _damageMatrix = damageMatrix;
            _modifiers = modifiers;
            Hull = data.Hull;
            Shields = data.Shields;
        }

        public void InitializeHardPoints(IReadOnlyList<HardPointModel> hardPointModels)
        {
            HardPointModels = hardPointModels.ToArray();
            foreach (HardPointModel hardPoint in HardPointModels)
            {
                HardPointHealth health = GetHardPointHealth(hardPoint.HardPointType);
                hardPoint.SetHealth(health.Health, health.HullDamageMultiplier);
            }
        }

        public void ApplyDamage(float damage, DamageType damageType, int hardPointId)
        {
            if (IsDestroyed)
            {
                return;
            }

            damage *= _modifiers.DamageTakenMultiplier;
            if (HasShields && !_damageMatrix.IsShieldPiercing(damageType))
            {
                Shields = Math.Max(0f, Shields - damage * _damageMatrix.GetShieldMultiplier(damageType));
            }
            else
            {
                float hullDamage = damage * _damageMatrix.GetDamageMultiplier(damageType, ShipClass);
                DamageHardPoint(HardPointModels[hardPointId], hullDamage);
            }

            OnValueChanged?.Invoke();
        }

        public void RegenerateShields(float value)
        {
            Shields = Math.Min(_data.Shields, Shields + value);
            OnValueChanged?.Invoke();
        }

        private void DamageHardPoint(HardPointModel hardPoint, float damage)
        {
            if (!hardPoint.IsDestroyed)
            {
                hardPoint.ApplyDamage(damage);
                if (hardPoint.IsDestroyed && hardPoint.HardPointType == HardPointType.ShieldGenerator)
                {
                    IsLostShieldGenerator = true;
                    Shields = 0f;
                }
            }

            Hull = Math.Max(0f, Hull - damage * hardPoint.HullDamageMultiplier);
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
