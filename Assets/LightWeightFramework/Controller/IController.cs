using LightWeightFramework.Model;

namespace LightWeightFramework.Controller
{
    public interface IController :IEntity
    {
        IModelObserver Model { get; }
    }
}
