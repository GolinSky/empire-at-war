using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using Zenject;

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

        private readonly IEntityFacade[] _facades;

        public long Id { get;  }

        public IHealthModelObserver HealthModel { get; }
        public PlayerId Owner { get; }

        public Entity(
            IHealthModelObserver healthModel,
            IEntityLocator entityLocator,
            IEntityFacade[] facades,
            PlayerId owner,
            long id)
        {
            _facades = facades;
            _entityLocator = entityLocator;
            Owner = owner;
            Id = id;
            HealthModel = healthModel;
        }

        public void Initialize()
        {
            _entityLocator.AddEntity(this);
        }

        public void LateDispose()
        {
            _entityLocator.RemoveEntity(this);
        }

        public bool TryGetFacade<TFacade>(out TFacade destinationFacade) where TFacade : IEntityFacade
        {
            destinationFacade = default;
            foreach (IEntityFacade entityFacade in _facades)
            {
                if (entityFacade is TFacade foundFacade)
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
