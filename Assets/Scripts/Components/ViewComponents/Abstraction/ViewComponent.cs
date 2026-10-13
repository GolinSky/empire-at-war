using EmpireAtWar.Mvc;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents
{
    public abstract class ViewComponent: EmpireAtWar.Mvc.ViewComponent
    {
        private IModelObserver InjectedModelObserver { get; set; }

        [Inject]
        private void Construct(IModelObserver modelObserver) => InjectedModelObserver = modelObserver;

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
