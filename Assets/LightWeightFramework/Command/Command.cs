using LightWeightFramework.Controller;

namespace LightWeightFramework.Command
{
    public abstract class Command : ICommand
    {

        protected Command(IController controller)
        {
        }
    }
    
    public abstract class Command<TController>:Command
        where TController:IController
    {
        protected TController Controller { get; }

        protected Command(TController controller):base(controller)
        {
            Controller = controller;
        }
    }
}