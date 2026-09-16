using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public class AttackData
    {
        public event Action UnitsChanged;
        private readonly IHealthModelObserver _shipUnitsProvider;
        private IHealthCommand HealthCommand { get; }

        public event Action Destroyed
        {
            add => _shipUnitsProvider.OnDestroy += value;
            remove => _shipUnitsProvider.OnDestroy -= value;
        }

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
                UnitsChanged?.Invoke();
                return Units is { Count: > 0 };
            }
            else
            {
                UnitsChanged?.Invoke();
                return false;
            }
        }
        
        public bool SameSource(AttackData other) =>
            HealthCommand != null && HealthCommand == other?.HealthCommand;
    }
}
