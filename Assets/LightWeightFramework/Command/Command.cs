using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Game;

namespace WorkShop.LightWeightFramework.Command
{
    public abstract class Command:ICommand
    {
        protected Command(IController controller)
        {
            
        }
        
    }
    
    public abstract class Command<TController>:Command
        where TController:IController
    {
        protected TController Controller { get; }
        protected IGameObserver GameObserver { get; }

        protected Command(TController controller, IGameObserver gameObserver):base(controller)
        {
            Controller = controller;
            GameObserver = gameObserver;
        }
    }
}