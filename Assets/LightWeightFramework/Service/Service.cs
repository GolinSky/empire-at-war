using LightWeightFramework.Controller;

namespace WorkShop.LightWeightFramework.Service
{
    public abstract class Service : IService
    {
        string IEntity.Id => GetType().Name;
    }
}