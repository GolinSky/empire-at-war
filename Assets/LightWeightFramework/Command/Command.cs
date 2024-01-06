using System.Collections.Generic;
using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Game;

namespace WorkShop.LightWeightFramework.Command
{
    public abstract class Command : ICommand
    {
        private List<ICommand> commands = new List<ICommand>();

        protected Command(IController entity)
        {
        }

        public bool TryGetCommand<TCommand>(out TCommand result) where TCommand : ICommand
        {
            foreach (var command in commands)
            {
                if (command is TCommand resultCommand)
                {
                    result = resultCommand;
                    return true;
                }
            }
            result = default;
            return false;
        }

        protected void AddCommand(ICommand command)
        {
            if(commands.Contains(command)) return;
            
            commands.Add(command);
        }
    }
    
    public abstract class Command<TController>:Command
        where TController:IController
    {
        protected TController Entity { get; }
        protected IGameObserver GameObserver { get; }

        protected Command(TController entity, IGameObserver gameObserver):base(entity)
        {
            Entity = entity;
            GameObserver = gameObserver;
        }
    }
}