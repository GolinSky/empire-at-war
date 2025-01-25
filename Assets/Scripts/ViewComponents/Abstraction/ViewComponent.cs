using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.ViewComponents
{
    public abstract class ViewComponent: LightWeightFramework.Components.ViewComponents.ViewComponent
    {
        [Inject]
        private IModelObserver InjectedModelObserver { get; }

        public override IModelObserver ModelObserver => InjectedModelObserver;
    }

    public class ViewComponent<TModel> : ViewComponent where TModel : IModelObserver
    {
        private  TModel _model;

        protected TModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = ModelObserver.GetModelObserver<TModel>();
                }
                return _model;
            }
        }
    }
}