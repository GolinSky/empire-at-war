namespace EmpireAtWar.Mvc
{
    public abstract class Service : IService
    {
        string IFrameworkObject.Id => GetType().Name;
    }
}
