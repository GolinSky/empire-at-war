namespace LightWeightFramework.Components.Service
{
    public abstract class Service : IService
    {
        string IEntity.Id => GetType().Name;
    }
}