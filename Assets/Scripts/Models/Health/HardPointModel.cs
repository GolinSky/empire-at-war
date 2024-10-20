using System;
using EmpireAtWar.Components.Ship.Health;
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

        public event Action OnShipUnitChanged;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health => health;

        public void SetHealth(float health)
        {
            originHealth = health;
            this.health = health;
            HealthPercentage = 1;
        }

        public void ApplyDamage(float damage)
        {
            health -= damage;
            HealthPercentage = health / originHealth;
            OnShipUnitChanged?.Invoke();
        }


#if UNITY_EDITOR
        public void SetDataFromEditor(int id, HardPointType hardPointType)
        {
            Id = id;
            HardPointType = hardPointType;
        }
#endif
    }
}