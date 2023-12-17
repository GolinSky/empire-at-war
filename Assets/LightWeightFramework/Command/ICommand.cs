namespace WorkShop.LightWeightFramework.Command
{
    public interface ICommand
    {
        bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand;
    }
}