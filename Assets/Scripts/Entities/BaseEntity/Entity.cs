using System;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Lifecycle;
using VContainer;
using VContainer.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntity
    {
        long Id { get; }
        IHealthModelObserver HealthModel { get; }

        PlayerId Owner { get; }

        bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade;

        TFacade GetFacade<TFacade>() where TFacade : IEntityFacade;
    }
    
    public class Entity: IEntity, IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entityLocator;
        private readonly IReadOnlyList<IEntityFacade> _facades;
        private readonly IReadOnlyList<IPooledEntityEvents> _poolEvents;
        private readonly IReadOnlyList<IHangarLaunchEvents> _hangarEvents;

        public long Id { get; }

        public IHealthModelObserver HealthModel { get; }
        public PlayerId Owner { get; }

        [Inject]
        public Entity(IHealthModelObserver healthModel, IEntityLocator entityLocator,
            ContainerLocal<IReadOnlyList<IEntityFacade>> facades, PlayerId owner, long id,
            ContainerLocal<IReadOnlyList<IPooledEntityEvents>> poolEvents,
            ContainerLocal<IReadOnlyList<IHangarLaunchEvents>> hangarEvents)
            : this(healthModel, entityLocator, facades.Value, owner, id, poolEvents.Value, hangarEvents.Value) { }

        public Entity(
            IHealthModelObserver healthModel,
            IEntityLocator entityLocator,
            IReadOnlyList<IEntityFacade> facades,
            PlayerId owner,
            long id,
            IReadOnlyList<IPooledEntityEvents> poolEvents,
            IReadOnlyList<IHangarLaunchEvents> hangarEvents)
        {
            _facades = facades;
            _entityLocator = entityLocator;
            _poolEvents = poolEvents;
            _hangarEvents = hangarEvents;
            Owner = owner;
            Id = id;
            HealthModel = healthModel;
        }

        public void Initialize()
        {
            foreach (IPooledEntityEvents pool in _poolEvents)
            {
                pool.Respawned += HandleRespawned;
                pool.Despawned += HandleDespawned;
            }
            foreach (IHangarLaunchEvents hangar in _hangarEvents)
                hangar.SquadronLaunched += HandleSquadronLaunched;
            _entityLocator.AddEntity(this);
        }

        public void LateDispose()
        {
            foreach (IPooledEntityEvents pool in _poolEvents)
            {
                pool.Respawned -= HandleRespawned;
                pool.Despawned -= HandleDespawned;
            }
            foreach (IHangarLaunchEvents hangar in _hangarEvents)
                hangar.SquadronLaunched -= HandleSquadronLaunched;
            _entityLocator.RemoveEntity(this);
        }

        private void HandleRespawned() => _entityLocator.AddEntity(this);

        private void HandleDespawned() => _entityLocator.RemoveEntity(this);

        private void HandleSquadronLaunched(ISquadron squadron) => squadron.Guard(this, Vector3.zero);

        public bool TryGetFacade<TFacade>(out TFacade destinationFacade) where TFacade : IEntityFacade
        {
            destinationFacade = default;
            // Indexed: foreach over IReadOnlyList boxes an enumerator on every lookup.
            for (int i = 0; i < _facades.Count; i++)
            {
                if (_facades[i] is TFacade foundFacade)
                {
                    destinationFacade = foundFacade;
                    return true;
                }
            }

            return false;
        }

        public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
        {
            if (TryGetFacade(out TFacade facade))
            {
                return facade;
            }

            throw new InvalidOperationException($"Entity {Id} has no {typeof(TFacade).Name}.");
        }
    }
}
