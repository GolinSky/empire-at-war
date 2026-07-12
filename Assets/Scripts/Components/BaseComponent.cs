using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components
{
    public class BaseComponent<TModel> : FrameworkComponent
        where TModel : IModelObserver
    {
        protected TModel Model { get; private set; }

        public BaseComponent(TModel model)
        {
            Model = model;
        }
    }
}
