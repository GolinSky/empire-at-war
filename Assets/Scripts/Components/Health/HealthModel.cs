using System;
using EmpireAtWar.Components.Combat;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    [Serializable]
    public class HealthModel : PureModel, IHealthState
    {
        private const float WEAPON_SYSTEM_COEFFICIENT = 0.8f;

        private readonly IHealthData _data;
        private readonly IDamageCalculator _damageCalculator;
        private readonly CombatModifiers _modifiers;
        private readonly float _armorBaseValue;
        private readonly float _shieldsBaseValue;

        public event Action OnValueChanged;
        public event Action OnDestroy;

        public float Armor { get; private set; }
        public float ArmorPercentage => _armorBaseValue <= 0f ? 0f : Armor / _armorBaseValue;
        public float Shields { get; private set; }
        public float ShieldRegenerateValue => _data.ShieldRegenerateValue;
        public float ShieldRegenerateDelay => _data.ShieldRegenerateDelay;
        public HardPointModel[] HardPointModels { get; private set; } = Array.Empty<HardPointModel>();
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0f;
        public float Dexterity => _data.Dexterity;
        public float ShieldPercentage => _shieldsBaseValue <= 0f ? 0f : Shields / _shieldsBaseValue;
        public bool IsLostShieldGenerator { get; private set; }
        public bool HasUnits => HardPointModels.Any(hardPoint => !hardPoint.IsDestroyed);

        public HealthModel(IHealthData data, IDamageCalculator damageCalculator,
            CombatModifiers modifiers)
        {
            _data = data;
            _damageCalculator = damageCalculator;
            _modifiers = modifiers;
            Armor = data.Armor;
            _armorBaseValue = Armor;
            Shields = data.Shields;
            _shieldsBaseValue = Shields;
        }

        public void InitializeHardPoints(IReadOnlyList<HardPointModel> hardPointModels)
        {
            if (hardPointModels == null)
            {
                throw new ArgumentNullException(nameof(hardPointModels));
            }

            HardPointModels = hardPointModels.ToArray();
            float totalCount = HardPointModels.Length;
            if (totalCount == 0)
            {
                return;
            }

            float mainSystemCount = HardPointModels.Count(hardPoint =>
                hardPoint.HardPointType == HardPointType.Engines ||
                hardPoint.HardPointType == HardPointType.ShieldGenerator);

            if (mainSystemCount == 0 || mainSystemCount == totalCount)
            {
                float healthPerUnit = Armor / totalCount;
                foreach (HardPointModel hardPoint in HardPointModels)
                {
                    hardPoint.SetHealth(healthPerUnit);
                }

                return;
            }

            float weaponCount = totalCount - mainSystemCount;
            float weaponHealth = Armor * WEAPON_SYSTEM_COEFFICIENT;
            float mainSystemHealth = Armor - weaponHealth;
            float weaponHealthPerUnit = weaponCount > 0f ? weaponHealth / weaponCount : 0f;
            float mainSystemHealthPerUnit = mainSystemCount > 0f ? mainSystemHealth / mainSystemCount : 0f;

            foreach (HardPointModel hardPoint in HardPointModels)
            {
                bool isMainSystem = hardPoint.HardPointType == HardPointType.Engines ||
                    hardPoint.HardPointType == HardPointType.ShieldGenerator;
                hardPoint.SetHealth(isMainSystem ? mainSystemHealthPerUnit : weaponHealthPerUnit);
            }
        }

        public void ApplyDamage(float damage, WeaponType weaponType, bool isMoving, int shipUnitId)
        {
            if (IsDestroyed)
            {
                return;
            }

            damage *= _modifiers.DamageTakenMultiplier;
            if (damage == 0f) return;
            DamageData damageData = _damageCalculator.GetDamage(weaponType, this, isMoving, damage);
            Shields -= damageData.ShieldDamage;
            Armor -= damageData.ArmorDamage;

            HardPointModel hardPointModel = HardPointModels[shipUnitId];
            float damageLeft = ApplyDamageOnShipUnit(hardPointModel, damageData.ArmorDamage);
            if (damageLeft > 0f)
            {
                ApplyDamageOnAllUnit(damageLeft);
            }

            if (Armor <= 0f)
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }

            OnValueChanged?.Invoke();
        }

        public void ApplyDamageOnAllUnit(float damage)
        {
            float damageLeft = damage;
            foreach (HardPointModel hardPointModel in HardPointModels)
            {
                if (hardPointModel.IsDestroyed)
                {
                    continue;
                }

                damageLeft = ApplyDamageOnShipUnit(hardPointModel, damageLeft);
                if (damageLeft == 0f)
                {
                    break;
                }
            }
        }

        public void RegenerateShields(float value)
        {
            Shields = Math.Min(_shieldsBaseValue, Shields + value);
            OnValueChanged?.Invoke();
        }

        private float ApplyDamageOnShipUnit(HardPointModel hardPointModel, float damage)
        {
            float damageLeft = hardPointModel.TryApplyDamage(damage);
            if (hardPointModel.HardPointType == HardPointType.ShieldGenerator && hardPointModel.IsDestroyed)
            {
                IsLostShieldGenerator = true;
                Shields = 0f;
            }

            return damageLeft;
        }
    }
}
