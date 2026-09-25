using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    [Serializable]
    public class HardPointModel : PureModel
    {
        public int Id { get; }
        public int Generation { get; private set; }
        public HardPointType HardPointType { get; }

        private float _originHealth;
        public event Action OnHardPointHealthChanged;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health { get; private set; }
        public float HullDamageMultiplier { get; private set; }
        public bool IsDestroyed => HealthPercentage <= 0f;

        public HardPointModel(int id, HardPointType hardPointType)
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
    }
}
