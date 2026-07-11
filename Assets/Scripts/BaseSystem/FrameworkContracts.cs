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
        TModelObserver GetModelObserver<TModelObserver>() where TModelObserver : IModelObserver;
    }

    public interface IModel : IModelObserver
    {
        TModelObserver GetModel<TModelObserver>() where TModelObserver : IModel;
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
