using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.SpaceStation
{
    /// <summary>
    /// Keeps <see cref="SecondStationLevelModel"/> in step with the owner's second station in the world.
    /// Runs on the host and on clients alike, since both see the replicated station.
    /// </summary>
    public sealed class SecondStationTracker : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly SecondStationLevelModel _model;
        private readonly PlayerSlot _owner;
        private IEntity _station;

        public SecondStationTracker(IEntityLocator entities, SecondStationLevelModel model, PlayerSlot owner)
        {
            _entities = entities;
            _model = model;
            _owner = owner;
        }

        public void Initialize()
        {
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
            if (_station != null) _station.HealthModel.OnDestroy -= HandleLost;
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (entity.Owner != _owner.Id || !entity.TryGetFacade(out ISecondStationFacade _) ||
                entity.HealthModel.IsDestroyed) return;
            _station = entity;
            _station.HealthModel.OnDestroy += HandleLost;
            _model.IsBuilt = true;
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity == _station) HandleLost();
        }

        private void HandleLost()
        {
            _station.HealthModel.OnDestroy -= HandleLost;
            _station = null;
            _model.HandleLost();
        }
    }
}
