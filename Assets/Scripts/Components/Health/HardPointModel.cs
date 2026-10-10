using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    [Serializable]
    public class HardPointModel : Model
    {
        private float _originHealth;

        public event Action OnHardPointHealthChanged;

        public int Id { get; }
        public int Generation { get; private set; }
        public HardPointType HardPointType { get; }
        /// <summary>Upgrade level that installs this hardpoint; until then it is absent and counts as destroyed.</summary>
        public int UnlockLevel { get; }
        public bool IsInstalled { get; private set; } = true;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health { get; private set; }
        public float MaxHealth => _originHealth;
        public float HullDamageMultiplier { get; private set; }
        public bool IsDestroyed => HealthPercentage <= 0f;

        public HardPointModel(HardPointType hardPointType, int id, int unlockLevel = 1)
        {
            Id = id;
            HardPointType = hardPointType;
            UnlockLevel = unlockLevel;
        }

        public void SetHealth(float health, float hullDamageMultiplier)
        {
            Generation++;
            IsInstalled = true;
            _originHealth = health;
            Health = health;
            HullDamageMultiplier = hullDamageMultiplier;
            HealthPercentage = health <= 0f ? 0f : 1f;
            OnHardPointHealthChanged?.Invoke();
        }

        public void Uninstall()
        {
            Generation++;
            IsInstalled = false;
            _originHealth = 0f;
            Health = 0f;
            HealthPercentage = 0f;
            OnHardPointHealthChanged?.Invoke();
        }

        public void ApplyDamage(float damage)
        {
            Health = Math.Max(0f, Health - damage);
            HealthPercentage = _originHealth <= 0f ? 0f : Health / _originHealth;
            OnHardPointHealthChanged?.Invoke();
        }

        public void Repair(float value)
        {
            if (IsDestroyed || Health >= MaxHealth || value <= 0f) return;
            Health = Math.Min(MaxHealth, Health + value);
            HealthPercentage = Health / MaxHealth;
            OnHardPointHealthChanged?.Invoke();
        }

        public void ScaleHealth(float multiplier)
        {
            _originHealth *= multiplier;
            Health *= multiplier;
            OnHardPointHealthChanged?.Invoke();
        }
    }
}
