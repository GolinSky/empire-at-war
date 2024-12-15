using EmpireAtWar.ViewComponents;
using LightWeightFramework.Model;
using LightWeightFramework.Components;
using LightWeightFramework.Command;
using UnityEngine;
using Zenject;
using ViewComponent = LightWeightFramework.Components.ViewComponents.ViewComponent;

namespace EmpireAtWar.Views.ViewImpl
{
    public abstract class View : BaseView, IInitializable, ILateDisposable
    {
        [field: SerializeField] public ModelDependency[] ModelDependencies { get; private set; }
        

        public virtual IModelObserver ModelObserver { get; }
        
        public void Initialize()
        {
            OnInitialize();
       
            foreach (ViewComponent viewComponent in viewComponents)
            {
                viewComponent.SetView(this);
                viewComponent.Init();
            }
            foreach (ModelDependency modelDependency in ModelDependencies)
            {
                modelDependency.Initialize(this);
            }
        }

        public void LateDispose()
        {
            OnDispose();
            foreach (ViewComponent viewComponent in viewComponents)
            {
                viewComponent.Dispose();
            }
            foreach (ModelDependency modelDependency in ModelDependencies)
            {
                modelDependency.Release();
            }
        }

        public void Release()
        {
            LateDispose();
        }

        protected abstract void OnInitialize();
        protected abstract void OnDispose();
    }

    public abstract class View<TModel> : View
        where TModel : IModelObserver
    {

        [Inject]
        public TModel Model { get; }

        public override IModelObserver ModelObserver => Model;

        protected override void OnInitialize()
        {
        }
    }

    public abstract class View<TModel, TCommand> : View<TModel>
        where TModel : IModelObserver
        where TCommand : ICommand
    {
        [Inject] protected TCommand Command { get; }
    }
}