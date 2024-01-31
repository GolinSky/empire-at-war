using LightWeightFramework.Model;

namespace LightWeightFramework.Controller
{
    public interface IController :IEntity
    {
        IModel GetModel();
    }
}
