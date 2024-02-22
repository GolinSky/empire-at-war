using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class AttackData
    {
        private readonly IShipUnitsProvider shipUnitsProvider;
        private IHealthComponent HealthComponent { get; }

        public bool IsDestroyed => HealthComponent == null || HealthComponent.Destroyed;
        public List<IShipUnitView> Units { get; private set; }
    

        public AttackData(IShipUnitsProvider shipUnitsProvider, IHealthComponent healthComponent, ShipUnitType shipUnitType)
        {
            this.shipUnitsProvider = shipUnitsProvider;
            Units = shipUnitsProvider.GetShipUnits(shipUnitType).ToList();
            HealthComponent = healthComponent;
        }

        public bool Contains(IShipUnitView shipUnitView)
        {
            return Units.Contains(shipUnitView);
        }

        public void ApplyDamage(float damage, WeaponType weaponType, int id)
        {
            HealthComponent.ApplyDamage(damage, weaponType, id);
        }

        public bool TryUpdateNewUnits(ShipUnitType shipUnitType = ShipUnitType.Any)
        {
            Units.Clear();
            Units = shipUnitsProvider.GetShipUnits(shipUnitType).ToList();
            return Units is { Count: > 0 };
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