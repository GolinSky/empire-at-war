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
        public ShipClass TargetClass => _shipUnitsProvider.ShipClass;
        public IHealthModelObserver TargetHealth => _shipUnitsProvider;
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

        /// <summary>Destroyed hardpoints are only valid aim points once the whole ship is a wreck.</summary>
        public bool CanTarget(IHardPointModel hardPointModel)
        {
            return !IsDestroyed && (!hardPointModel.IsDestroyed || !_shipUnitsProvider.HasLiveHardPoints);
        }

        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            HealthCommand.ApplyDamage(damage, damageType, id);
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
