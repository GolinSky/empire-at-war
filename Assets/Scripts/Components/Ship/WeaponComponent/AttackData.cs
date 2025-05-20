using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Weapon;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class AttackData
    {
        private readonly IHealthModelObserver _shipUnitsProvider;
        private IHealthCommand HealthCommand { get; }

        public bool IsDestroyed => _shipUnitsProvider == null || _shipUnitsProvider.IsDestroyed;
        public List<IHardPointModel> Units { get; private set; }
    

        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthCommand healthCommand, HardPointType hardPointType)
        {
            _shipUnitsProvider = shipUnitsProvider;
            Units = shipUnitsProvider.GetShipUnits(hardPointType).ToList();
            HealthCommand = healthCommand;
        }

        public bool Contains(IHardPointModel hardPointModel)
        {
            return Units.Contains(hardPointModel);
        }

        public void ApplyDamage(float damage, WeaponType weaponType, int id)
        {
            HealthCommand.ApplyDamage(damage, weaponType, id);
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
            if (a?.HealthCommand == null || b?.HealthCommand == null) return false;
            
            return a.HealthCommand.Equals(b.HealthCommand);
        }

        public static bool operator !=(AttackData a, AttackData b)
        {
            return !(a == b);
        }
    }
}