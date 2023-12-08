using LightWeightFramework.Model;
using UnityEngine;
namespace WorkShop.LightWeightFramework.ViewComponents
{
    public abstract class ViewComponent:MonoBehaviour
    {
        protected IModelObserver ModelObserver { get; private set; }
        protected View View { get; private set; }
        
        public void Init(View view)
        {
            View = view;
            ModelObserver = view.ModelObserver;
            OnInit();
        }
        
        public void Release()
        {
            OnRelease();
        }

        protected abstract void OnInit();
        protected abstract void OnRelease();
    }

    public abstract class ViewComponent<TModel>:ViewComponent
        where TModel: IModelObserver
    {
        protected TModel Model { get; private set; }
        protected override void OnInit()
        {
            Model = ModelObserver.GetModelObserver<TModel>();
        }
    }
    
}