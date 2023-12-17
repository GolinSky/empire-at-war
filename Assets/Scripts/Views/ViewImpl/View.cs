using LightWeightFramework.Model;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.ViewComponents;
using Zenject;

namespace EmpireAtWar.Views.ViewImpl
{
    public abstract class View<TModel>:View, IInitializable, ILateDisposable
        where TModel:IModelObserver
    {
        [Inject]
        protected TModel Model { get; private set; }
        
        public void LateDispose()
        {
            Release();
            OnDispose();
        }

        protected abstract void OnInitialize();
        protected abstract void OnDispose();
        public virtual void Initialize()
        {
            OnInitialize();
        }
    }

    public abstract class View<TModel, TCommand>:View<TModel>
        where TModel : IModelObserver
        where TCommand : ICommand
    {
        [Inject]
        protected TCommand Command { get; }

        public sealed override void Initialize()
        {
            base.Initialize();
            foreach (ViewComponent viewComponent in viewComponents)
            {
                viewComponent.SetCommand(Command);
            }
        }
    }
}