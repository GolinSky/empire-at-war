namespace EmpireAtWar.Mvc
{
    public abstract class FrameworkComponent : IComponent
    {
        string IFrameworkObject.Id => GetType().Name;
    }
}
