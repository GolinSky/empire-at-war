using LightWeightFramework.Model;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Views.ViewImpl
{
    public abstract class View<TModel>:View, IInitializable, ILateDisposable
        where TModel:IModelObserver
    {
        [Inject]
        protected TModel Model { get; private set; }

        // [Inject]
        // public void Construct(TModel model)
        // {
        //     Model = model;
        //     Debug.Log("InjectedView Initialize");
        //     Init(Model);
        // }

        public void LateDispose()
        {
            Release();
            OnDispose();
        }

        protected abstract void OnInitialize();
        protected abstract void OnDispose();
        public void Initialize()
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
    }
}