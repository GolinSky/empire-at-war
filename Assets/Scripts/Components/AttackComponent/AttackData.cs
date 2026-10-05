using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public class AttackData
    {
        private readonly IHealthModelObserver _shipUnitsProvider;
        private readonly HardPointType _hardPointType;
        private readonly bool _isFocused;

        public event Action UnitsChanged;

        public event Action Destroyed
        {
            add => _shipUnitsProvider.OnDestroy += value;
            remove => _shipUnitsProvider.OnDestroy -= value;
        }

        private IHealthFacade HealthFacade { get; }

        public bool IsDestroyed => _shipUnitsProvider == null || _shipUnitsProvider.IsDestroyed;
        public ShipClass TargetClass => _shipUnitsProvider.ShipClass;
        public IHealthModelObserver TargetHealth => _shipUnitsProvider;
        public List<IHardPointModel> Units { get; private set; }

        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthFacade healthFacade, HardPointType hardPointType)
        {
            _shipUnitsProvider = shipUnitsProvider;
            _hardPointType = hardPointType;
            Units = shipUnitsProvider.GetShipUnits(hardPointType).ToList();
            HealthFacade = healthFacade;
        }

        /// <summary>Focuses fire on one hardpoint the player picked explicitly.</summary>
        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthFacade healthFacade, IHardPointModel hardPoint)
        {
            _shipUnitsProvider = shipUnitsProvider;
            _isFocused = true;
            Units = new List<IHardPointModel> { hardPoint };
            HealthFacade = healthFacade;
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

        /// <summary>
        /// Re-reads the target's hardpoints once none of the listed ones can be hit while the target still has
        /// live ones elsewhere, e.g. hardpoints restored or installed by a station upgrade.
        /// </summary>
        public void RefreshStaleUnits()
        {
            if (_isFocused || IsDestroyed || Units.Exists(CanTarget))
            {
                return;
            }

            Units = _shipUnitsProvider.GetShipUnits(_hardPointType).ToList();
        }

        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            HealthFacade.ApplyDamage(damage, damageType, id);
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
            HealthFacade != null && HealthFacade == other?.HealthFacade;
    }
}
