using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IShipUnitModel
    {
        event Action OnShipUnitChanged;
        HardPointType HardPointType { get; }

        float HealthPercentage { get; }
        int Id { get; }
    }

    [Serializable]
    public class HardPointModel : IShipUnitModel
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public HardPointType HardPointType { get; private set; }

        private float originHealth;
        private float health;
        private HardPointView _hardPointView;

        public event Action OnShipUnitChanged;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health => health;
        
        public bool IsDestroyed => HealthPercentage <= 0f;

        public void SetHealth(float health)
        {
            originHealth = health;
            this.health = health;
            HealthPercentage = health;
            _hardPointView.UpdateData(HealthPercentage);
        }

        public void ApplyDamage(float damage)
        {
            health -= damage;
            HealthPercentage = health / originHealth;
            OnShipUnitChanged?.Invoke();
            _hardPointView.UpdateData(HealthPercentage);
        }


#if UNITY_EDITOR
        public void SetDataFromEditor(int id, HardPointType hardPointType)
        {
            Id = id;
            HardPointType = hardPointType;
        }
#endif
        public void SetData(HardPointView hardPointView)
        {
            _hardPointView = hardPointView;
            Id = hardPointView.Id;
            HardPointType = hardPointView.HardPointType;
        }

        public float TryApplyDamage(float damage)
        {
            if (health >= damage)
            {
                ApplyDamage(damage);
                return 0.0f;
            }

            float damageLeft = damage - health;
            ApplyDamage(health);
            return damageLeft;
        }
    }
}