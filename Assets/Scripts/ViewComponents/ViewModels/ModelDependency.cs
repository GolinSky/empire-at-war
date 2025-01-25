using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.ViewComponents
{
    public abstract class ModelDependency : MonoBehaviour
    {
        protected View View { get; private set; }

        public virtual void Initialize(View view)
        {
            View = view;
        }
        
        public virtual void Release()
        {
        }
    }

    /// <summary>
    /// Monobehaviour class for view model - store data and injected dependencies into model
    /// </summary>
    public abstract class ModelDependency<TModel> : ModelDependency where TModel : IModelObserver
    {
        protected TModel Model { get; private set; }


        public sealed override void Initialize(View view)
        {
            base.Initialize(view);
            Model = View.ModelObserver.GetModelObserver<TModel>();
            OnInit();
        }

        public sealed override void Release()
        {
            base.Release();
            OnDispose();
        }

        protected virtual void OnInit() {}
        
        protected virtual void OnDispose() {}
    }
}