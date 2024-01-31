using LightWeightFramework.Controller;

namespace WorkShop.LightWeightFramework.Components
{
    public abstract class Component : IComponent
    {
        string IEntity.Id => GetType().Name;
    }
}