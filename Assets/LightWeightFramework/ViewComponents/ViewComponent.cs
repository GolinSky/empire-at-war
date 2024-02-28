using LightWeightFramework.Model;
using UnityEngine;

namespace LightWeightFramework.Components.ViewComponents
{
    public abstract class ViewComponent:MonoBehaviour
    {
        public IModelObserver ModelObserver { get; private set; }
        protected View View { get; private set; }
        
        public void Init(View view)
        {
            Initialize(view);
            OnInit();
        }

        protected virtual void Initialize(View view)
        {
            View = view;
            ModelObserver = view.Model;
        }

        public void Release()
        {
            OnRelease();
        }
        
        protected virtual void OnInit(){}
        protected virtual void OnRelease(){}
    }

    public abstract class ViewComponent<TModel>:ViewComponent
        where TModel: IModelObserver
    {
        protected TModel Model { get; private set; }

        protected sealed override void Initialize(View view)
        {
            base.Initialize(view);
            Model = ModelObserver.GetModelObserver<TModel>();
        }
    }
}