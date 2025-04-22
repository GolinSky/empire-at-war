using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Weapon;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class AttackData
    {
        private readonly IHealthModelObserver _shipUnitsProvider;
        public IHealthComponent HealthComponent { get; }

        public bool IsDestroyed => _shipUnitsProvider == null || _shipUnitsProvider.IsDestroyed;
        public List<IHardPointModel> Units { get; private set; }
    

        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthComponent healthComponent, HardPointType hardPointType)
        {
            _shipUnitsProvider = shipUnitsProvider;
            Units = shipUnitsProvider.GetShipUnits(hardPointType).ToList();
            HealthComponent = healthComponent;
        }

        public bool Contains(IHardPointModel hardPointModel)
        {
            return Units.Contains(hardPointModel);
        }

        public void ApplyDamage(float damage, WeaponType weaponType, int id)
        {
            HealthComponent.ApplyDamage(damage, weaponType, id);
        }

        public bool TryUpdateNewUnits(HardPointType hardPointType = HardPointType.Any)
        {
            Units.Clear();
            if (_shipUnitsProvider.HasUnits)
            {
                Units = _shipUnitsProvider.GetShipUnits(hardPointType).ToList();
                return Units is { Count: > 0 };
            }
            else
            {
                return false;
            }
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