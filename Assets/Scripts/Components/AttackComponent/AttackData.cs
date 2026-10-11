using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public class AttackData
    {
        private readonly IHealthModelObserver _shipUnitsProvider;
        private readonly HardPointType _hardPointType;
        private readonly bool _isFocused;
        private readonly IEntity _targetEntity;

        public bool CanAcquireTarget => !_targetEntity.IsCloaked();

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
        public IEntity TargetEntity => _targetEntity;
        public List<IHardPointModel> Units { get; }

        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthFacade healthFacade, HardPointType hardPointType, IEntity targetEntity)
        {
            _targetEntity = targetEntity;
            _shipUnitsProvider = shipUnitsProvider;
            _hardPointType = hardPointType;
            Units = new List<IHardPointModel>(shipUnitsProvider.GetShipUnits(hardPointType));
            HealthFacade = healthFacade;
        }

        /// <summary>Focuses fire on one hardpoint the player picked explicitly.</summary>
        public AttackData(IHealthModelObserver shipUnitsProvider, IHealthFacade healthFacade, IHardPointModel hardPoint, IEntity targetEntity)
        {
            _targetEntity = targetEntity;
            _shipUnitsProvider = shipUnitsProvider;
            _isFocused = true;
            Units = new List<IHardPointModel> { hardPoint };
            HealthFacade = healthFacade;
        }

        // Runs per shot: reference compares skip List.Contains' EqualityComparer dispatch.
        public bool Contains(IHardPointModel hardPointModel)
        {
            for (int i = 0; i < Units.Count; i++)
                if (ReferenceEquals(Units[i], hardPointModel)) return true;
            return false;
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
            if (_isFocused || IsDestroyed || HasTargetableUnit())
            {
                return;
            }

            ReplaceUnits(_hardPointType);
        }

        // Refills the existing list so a refresh allocates only the provider's array.
        private void ReplaceUnits(HardPointType hardPointType)
        {
            Units.Clear();
            Units.AddRange(_shipUnitsProvider.GetShipUnits(hardPointType));
        }

        // A plain loop: List.Exists(CanTarget) allocates a predicate on every candidate rebuild.
        private bool HasTargetableUnit()
        {
            for (int i = 0; i < Units.Count; i++)
                if (CanTarget(Units[i])) return true;
            return false;
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
                ReplaceUnits(hardPointType);
                UnitsChanged?.Invoke();
                return Units is { Count: > 0 };
            }
            else
            {
                UnitsChanged?.Invoke();
                return false;
            }
        }

        public bool IsFrom(IHealthFacade healthFacade) =>
            HealthFacade != null && HealthFacade == healthFacade;

        public bool SameSource(AttackData other) =>
            HealthFacade != null && HealthFacade == other?.HealthFacade;
    }
}
