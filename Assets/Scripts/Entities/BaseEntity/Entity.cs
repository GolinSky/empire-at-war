using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Ship;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntity
    {
        long Id { get; }
        bool TryGetCommand<TCommand>(out TCommand entityCommand) where TCommand : IEntityCommand;
        IModelObserver Model { get; }
        IHealthModelObserver HealthModel { get; }
        
        PlayerType PlayerType { get; }
    }
    
    public class Entity: IEntity, IInitializable, ILateDisposable
    {
        private readonly IEntityCommand[] _commands;
        private readonly IEntityLocator _entityLocator;
        public long Id { get;  }
        
        public IModelObserver Model { get; }
        public IHealthModelObserver HealthModel { get; }
        public PlayerType PlayerType { get; }

        public Entity(
            long id,
            IEntityCommand[] commands,
            IUnitModelObserver modelObserver,
            IHealthModelObserver healthModel,
            IEntityLocator entityLocator,
            PlayerType playerType)
        {
            _commands = commands;
            _entityLocator = entityLocator;
            PlayerType = playerType;
            Id = id;
            Model = modelObserver;
            HealthModel = healthModel;
        }
        
        public bool TryGetCommand<TCommand>(out TCommand destinationCommand) where TCommand : IEntityCommand
        {
            destinationCommand = default;
            foreach (IEntityCommand entityCommand in _commands)
            {
                if (entityCommand is TCommand foundCommand)
                {
                    destinationCommand = foundCommand;
                    return true;
                }
            }
            
            return false;
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
