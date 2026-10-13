using EmpireAtWar.Lifecycle;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Stations
{
    /// <summary>Tracks space stations and production bases registered in the <see cref="IEntityLocator"/>.</summary>
    public sealed class StationRegistry : Service, IStationRegistry, IOrderedDisposable
    {
        private readonly IEntityLocator _entityLocator;

        private readonly List<IEntity> _stations = new List<IEntity>();
        private readonly List<IEntity> _productionBases = new List<IEntity>();

        public StationRegistry(IEntityLocator entityLocator)
        {
            _entityLocator = entityLocator;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                HandleEntityAdded(entity);
            }

            _entityLocator.EntityAdded += HandleEntityAdded;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void Dispose()
        {
            _entityLocator.EntityAdded -= HandleEntityAdded;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
        }

        public bool HasProductionBase(PlayerId owner)
        {
            foreach (IEntity productionBase in _productionBases)
            {
                if (productionBase.Owner == owner &&
                    !productionBase.HealthModel.IsDestroyed &&
                    productionBase.HealthModel.HasUnits)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetLivingStation(PlayerId owner, out IEntity station)
        {
            foreach (IEntity candidate in _stations)
            {
                if (candidate.Owner == owner && !candidate.HealthModel.IsDestroyed)
                {
                    station = candidate;
                    return true;
                }
            }

            station = null;
            return false;
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (entity.IsPlayerBase())
            {
                _stations.Add(entity);
            }

            if (entity.TryGetFacade(out IProductionBaseFacade _))
            {
                _productionBases.Add(entity);
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            _stations.Remove(entity);
            _productionBases.Remove(entity);
        }
    }
}
