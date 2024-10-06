using LightWeightFramework.Model;
using LightWeightFramework.Components.Components;

namespace EmpireAtWar.Components
{
    public class BaseComponent<TModel> : Component
        where TModel : IModel
    {
        protected TModel Model { get; private set; }

        public BaseComponent(IModel model)
        {
            Model = model.GetModel<TModel>();
        }
    }
}