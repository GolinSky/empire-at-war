using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Command;

namespace LightWeightFramework.Controller
{
    public interface IController :IEntity
    {
        IModelObserver Model { get; } }
}
