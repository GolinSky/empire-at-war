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
        private readonly HardPointHealthBudget _hardPointHealthBudget;
        private float _hullMultiplier;
        private float _shieldsMultiplier;
        private int _level = 1;
        private float _levelHullScale = 1f;
        private float _levelShieldsScale = 1f;
        private float _levelShieldRegenerateScale = 1f;
        private float _levelHardPointScale = 1f;
        private float _constructionScale = 1f;

        public event Action OnValueChanged;

        public event Action OnDestroy;

        public event Action OnRestored;

        public ShipClass ShipClass => _data.ShipClass;
        public float Hull { get; private set; }
        public float MaxHull => _data.Hull * _hullMultiplier * _levelHullScale * _constructionScale;
        public float HullPercentage => MaxHull <= 0f ? 0f : Hull / MaxHull;
        public float Shields { get; private set; }
        public float ShieldPercentage => MaxShields <= 0f ? 0f : Shields / MaxShields;
        public float MaxShields => _constructionScale < 1f ? 0f : _data.Shields * _shieldsMultiplier * _levelShieldsScale;
        public float ShieldRegenerateValue => _constructionScale < 1f ? 0f : _data.ShieldRegenerateValue * _levelShieldRegenerateScale;
        public float ShieldRegenerateDelay => _data.ShieldRegenerateDelay;
        public HardPointModel[] HardPointModels { get; private set; } = Array.Empty<HardPointModel>();
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0f;
        public bool IsLostShieldGenerator { get; private set; }
        public bool IsShieldSuppressed { get; private set; }
        public bool HasUnits => !IsDestroyed;
        // Queried per target candidate; a loop avoids the LINQ enumerator allocation.
        public bool HasLiveHardPoints
        {
            get
            {
                foreach (HardPointModel hardPoint in HardPointModels)
                    if (!hardPoint.IsDestroyed) return true;
                return false;
            }
        }

        public HealthModel(IHealthData data, DamageMatrixData damageMatrix, CombatModifiers modifiers,
            HardPointHealthBudget hardPointHealthBudget)
        {
            _data = data;
            _damageMatrix = damageMatrix;
            _modifiers = modifiers;
            _hardPointHealthBudget = hardPointHealthBudget;
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
            Shields = IsShieldSuppressed ? 0f : Shields * _modifiers.ShieldsMultiplier / _shieldsMultiplier;
            _hullMultiplier = _modifiers.HullMultiplier;
            _shieldsMultiplier = _modifiers.ShieldsMultiplier;
            if (_data.UsesHullHardPointBudget)
            {
                float[] health = _hardPointHealthBudget.Calculate(MaxHull, ShipClass, HardPointModels, _level);
                for (int i = 0; i < HardPointModels.Length; i++)
                    if (HardPointModels[i].IsInstalled)
                        HardPointModels[i].ScaleHealth(health[i] / HardPointModels[i].MaxHealth);
            }
            OnValueChanged?.Invoke();
        }

        public void InitializeHardPoints(IReadOnlyList<HardPointModel> hardPointModels)
        {
            HardPointModels = hardPointModels.ToArray();
            float[] budget = _data.UsesHullHardPointBudget
                ? _hardPointHealthBudget.Calculate(MaxHull, ShipClass, HardPointModels, _level)
                : Array.Empty<float>();
            for (int i = 0; i < HardPointModels.Length; i++)
            {
                HardPointModel hardPoint = HardPointModels[i];
                if (hardPoint.UnlockLevel > _level)
                {
                    hardPoint.Uninstall();
                    continue;
                }

                if (_data.UsesHullHardPointBudget)
                    hardPoint.SetHealth(budget[i], 1f);
                else
                {
                    HardPointHealth health = GetHardPointHealth(hardPoint.HardPointType);
                    hardPoint.SetHealth((hardPoint.HealthOverride > 0f ? hardPoint.HealthOverride : health.Health) * _levelHardPointScale * _constructionScale,
                        health.HullDamageMultiplier);
                }
            }
        }

        /// <summary>Builds new hull and hardpoint capacity without repairing damage already taken.</summary>
        public void SetConstructionProgress(float progress)
        {
            if (IsDestroyed) return;
            float scale = 0.1f + 0.9f * Math.Clamp(progress, 0f, 1f);
            if (_constructionScale == scale) return;

            float missingHull = MaxHull - Hull;
            float previousScale = _constructionScale;
            _constructionScale = scale;
            Hull = Math.Max(0f, MaxHull - missingHull);
            float[] budget = _data.UsesHullHardPointBudget
                ? _hardPointHealthBudget.Calculate(MaxHull, ShipClass, HardPointModels, _level)
                : Array.Empty<float>();
            for (int i = 0; i < HardPointModels.Length; i++)
                HardPointModels[i].SetConstructionMaxHealth(_data.UsesHullHardPointBudget
                    ? budget[i]
                    : HardPointModels[i].MaxHealth * scale / previousScale);

            Shields = scale < 1f || IsShieldSuppressed || IsLostShieldGenerator ? 0f : MaxShields;
            OnValueChanged?.Invoke();
        }

        /// <summary>
        /// Raises the unit to an upgrade level: hull, shields and shield regeneration take the level's scales
        /// (current hull and shields keep their percentage), hardpoints unlocked by the level are installed
        /// and every installed hardpoint, the shield generator included, is restored to full health.
        /// </summary>
        public void Upgrade(int level, float hullScale, float shieldsScale, float shieldRegenerateScale, float hardPointScale = 1f)
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
            _levelHardPointScale = hardPointScale;
            Hull = MaxHull * hullPercentage;
            Shields = IsShieldSuppressed ? 0f : MaxShields * shieldPercentage;
            IsLostShieldGenerator = false;
            InitializeHardPoints(HardPointModels);
            OnValueChanged?.Invoke();
        }

        /// <summary>
        /// Applies saved fractions of hull, shields and each hardpoint. A lost shield generator drops the shields,
        /// as it does in combat. The save must come from the same hardpoint layout.
        /// </summary>
        public void Restore(float hullPercentage, float shieldPercentage, IReadOnlyList<float> hardPointPercentages)
        {
            if (hardPointPercentages.Count != HardPointModels.Length)
                throw new InvalidOperationException(
                    $"Saved {hardPointPercentages.Count} hardpoints; {ShipClass} has {HardPointModels.Length}.");
            for (int i = 0; i < HardPointModels.Length; i++)
                HardPointModels[i].Restore(hardPointPercentages[i]);
            IsLostShieldGenerator = HardPointModels.Any(point => point.HardPointType == HardPointType.ShieldGenerator) &&
                HardPointModels.Where(point => point.HardPointType == HardPointType.ShieldGenerator)
                    .All(point => point.IsDestroyed);
            Hull = MaxHull * hullPercentage;
            Shields = IsLostShieldGenerator || IsShieldSuppressed ? 0f : MaxShields * shieldPercentage;
            OnRestored?.Invoke();
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
                Shields = Math.Max(0f, Shields - damage * _damageMatrix.GetShipShieldMultiplier(damageType));
            }
            else
            {
                float hullDamage = damage * _damageMatrix.GetDamageMultiplier(damageType, ShipClass);
                if (HardPointModels.Length == 0)
                {
                    DamageHull(hullDamage);
                }
                else if (HardPointModels[hardPointId].IsDestroyed && HasLiveHardPoints)
                {
                    DamageLiveHardPoints(hullDamage);
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
            if (IsDestroyed || IsShieldSuppressed) return;
            Shields = Math.Min(MaxShields, Shields + value);
            OnValueChanged?.Invoke();
        }

        public bool AbsorbsDamage(DamageType damageType)
        {
            return !IsDestroyed && HasShields && !IsLostShieldGenerator && !IsShieldSuppressed &&
                   !_damageMatrix.IsShieldPiercing(damageType);
        }

        public void SetShieldSuppressed(bool suppressed)
        {
            if (IsShieldSuppressed == suppressed) return;
            IsShieldSuppressed = suppressed;
            if (suppressed) Shields = 0f;
            OnValueChanged?.Invoke();
        }

        // A hit on an already destroyed hardpoint is split evenly between the live ones.
        private void DamageLiveHardPoints(float damage)
        {
            int liveCount = 0;
            foreach (HardPointModel hardPoint in HardPointModels)
                if (!hardPoint.IsDestroyed) liveCount++;

            float share = damage / liveCount;
            foreach (HardPointModel hardPoint in HardPointModels)
            {
                if (hardPoint.IsDestroyed) continue;
                DamageHardPoint(hardPoint, share);
                if (IsDestroyed) return;
            }
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
