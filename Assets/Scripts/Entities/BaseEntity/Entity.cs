using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Ship;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntity
    {
        long Id { get; }
        bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade;
        TFacade GetFacade<TFacade>() where TFacade : IEntityFacade;
        IModelObserver Model { get; }
        IHealthModelObserver HealthModel { get; }
        
        PlayerId Owner { get; }
    }
    
    public class Entity: IEntity, IInitializable, ILateDisposable
    {
        private readonly IEntityFacade[] _facades;
        private readonly IEntityLocator _entityLocator;
        public long Id { get;  }
        
        public IModelObserver Model { get; }
        public IHealthModelObserver HealthModel { get; }
        public PlayerId Owner { get; }

        public Entity(
            long id,
            IEntityFacade[] facades,
            IUnitModelObserver modelObserver,
            IHealthModelObserver healthModel,
            IEntityLocator entityLocator,
            PlayerId owner)
        {
            _facades = facades;
            _entityLocator = entityLocator;
            Owner = owner;
            Id = id;
            Model = modelObserver;
            HealthModel = healthModel;
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

        public void Initialize()
        {
            _entityLocator.AddEntity(this);
        }

        public void LateDispose()
        {
            _entityLocator.RemoveEntity(this);
        }
    }
}
