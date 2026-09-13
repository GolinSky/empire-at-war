using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    [Serializable]
    public class HardPointModel : PureModel
    {
        public int Id { get; }
        public HardPointType HardPointType { get; }

        private float _originHealth;
        private float _health;
        public event Action OnHardPointHealthChanged;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health => _health;
        public bool IsDestroyed => HealthPercentage <= 0f;

        public HardPointModel(int id, HardPointType hardPointType)
        {
            Id = id;
            HardPointType = hardPointType;
        }

        public void SetHealth(float health)
        {
            _originHealth = health;
            _health = health;
            HealthPercentage = health <= 0f ? 0f : 1f;
            OnHardPointHealthChanged?.Invoke();
        }

        public void ApplyDamage(float damage)
        {
            _health -= damage;
            HealthPercentage = _originHealth <= 0f ? 0f : _health / _originHealth;
            OnHardPointHealthChanged?.Invoke();
        }

        public float TryApplyDamage(float damage)
        {
            if (_health >= damage)
            {
                ApplyDamage(damage);
                return 0.0f;
            }

            float damageLeft = damage - _health;
            ApplyDamage(_health);
            return damageLeft;
        }
    }
}
