namespace EmpireAtWar.Mvc
{
    public abstract class FrameworkComponent : IComponent
    {
        string IEntity.Id => GetType().Name;
    }

    public abstract class Service : IService
    {
        string IEntity.Id => GetType().Name;
    }
}
