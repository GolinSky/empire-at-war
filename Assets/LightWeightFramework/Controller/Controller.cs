using LightWeightFramework.Model;


namespace LightWeightFramework.Controller
{
    public abstract class Controller : IController
    {
        public virtual string Id => GetType().Name;
        public abstract IModel GetModel();
    }

    public abstract class Controller<TModel> : Controller, IController
        where TModel : IModel
    {
        protected TModel Model { get; }

        protected Controller(TModel model)
        {
            Model = model;
        }

        public override IModel GetModel()
        {
            return Model;
        }
    }
}