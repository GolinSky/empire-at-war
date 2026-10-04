using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    [Serializable]
    public class HardPointModel : PureModel
    {
        private float _originHealth;

        public event Action OnHardPointHealthChanged;

        public int Id { get; }
        public int Generation { get; private set; }
        public HardPointType HardPointType { get; }
        public float HealthPercentage { get; private set; } = 1f;

        public float Health { get; private set; }
        public float MaxHealth => _originHealth;
        public float HullDamageMultiplier { get; private set; }
        public bool IsDestroyed => HealthPercentage <= 0f;

        public HardPointModel(HardPointType hardPointType, int id)
        {
            Id = id;
            HardPointType = hardPointType;
        }

        public void SetHealth(float health, float hullDamageMultiplier)
        {
            Generation++;
            _originHealth = health;
            Health = health;
            HullDamageMultiplier = hullDamageMultiplier;
            HealthPercentage = health <= 0f ? 0f : 1f;
            OnHardPointHealthChanged?.Invoke();
        }

        public void ApplyDamage(float damage)
        {
            Health = Math.Max(0f, Health - damage);
            HealthPercentage = _originHealth <= 0f ? 0f : Health / _originHealth;
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
