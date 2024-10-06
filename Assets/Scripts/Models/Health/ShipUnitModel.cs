using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IShipUnitModel
    {
        event Action OnShipUnitChanged;
        ShipUnitType ShipUnitType { get; }

        float HealthPercentage { get; }
        int Id { get; }
    }

    [Serializable]
    public class ShipUnitModel : IShipUnitModel
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public ShipUnitType ShipUnitType { get; private set; }

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
    }
}