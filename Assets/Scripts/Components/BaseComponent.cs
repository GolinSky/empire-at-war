using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components
{
    public class BaseComponent<TModel> : FrameworkComponent
        where TModel : IModel
    {
        protected TModel Model { get; private set; }

        public BaseComponent(IModel model)
        {
            Model = model.GetModel<TModel>();
        }
    }
}