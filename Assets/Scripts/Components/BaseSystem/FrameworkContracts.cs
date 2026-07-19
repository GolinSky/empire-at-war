namespace EmpireAtWar.Mvc
{
    public interface IEntity
    {
        string Id { get; }
    }

    public interface ICommand
    {
    }

    public interface IModelObserver
    {
    }

    public interface IModel : IModelObserver
    {
    }

    public interface IController : IEntity
    {
        IModel GetModel();
    }

    public interface IComponent : IEntity
    {
    }

    public interface IService : IEntity
    {
    }
}
