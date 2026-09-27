namespace EmpireAtWar.Mvc
{
    public abstract class Controller : IController
    {
        public virtual string Id => GetType().Name;

        public abstract IModel GetModel();
    }

    public abstract class Controller<TModel> : Controller where TModel : IModel
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
