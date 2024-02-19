using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class AttackData
    {
        private IHealthComponent HealthComponent { get; }
        private IShipUnitView ShipUnitView { get;  set; }

        public bool IsDestroyed => HealthComponent == null ||  HealthComponent.Destroyed;
        public Vector3 Position => ShipUnitView.Position;
        
        public AttackData(IShipUnitView shipUnitView, IHealthComponent healthComponent)
        {
            ShipUnitView = shipUnitView;
            HealthComponent = healthComponent;
        }

        public void ApplyDamage(float damage, WeaponType weaponType)
        {
            HealthComponent.ApplyDamage(damage, weaponType, ShipUnitView.Id);
        }

        public void UpdateData(AttackData data)
        {
            ShipUnitView = data.ShipUnitView;
          //  Debug.Log($"UpdateData: {old} -> {ShipUnitView.Id.ToString()}");
        }

        public static bool operator ==(AttackData a, AttackData b)
        {
            if (a?.HealthComponent == null || b?.HealthComponent == null) return false;
            
            return a.HealthComponent.Equals(b.HealthComponent);
        }

        public static bool operator !=(AttackData a, AttackData b)
        {
            return !(a == b);
        }
    }
}