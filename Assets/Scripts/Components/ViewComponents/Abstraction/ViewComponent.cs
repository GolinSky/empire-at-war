using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.ViewComponents
{
    public abstract class ViewComponent: EmpireAtWar.Mvc.ViewComponent
    {
        [Inject]
        private IModelObserver InjectedModelObserver { get; }

        public override IModelObserver ModelObserver => InjectedModelObserver;
    }

    public class ViewComponent<TModel> : ViewComponent where TModel : class, IModelObserver
    {
        private  TModel _model;

        protected TModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = ModelObserver as TModel;
                }
                return _model;
            }
        }
    }
}
