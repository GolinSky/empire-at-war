using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;

namespace WorkShop.LightWeightFramework.ViewComponents
{
    public abstract class ViewComponent:MonoBehaviour
    {
        protected IModelObserver ModelObserver { get; private set; }
        protected View View { get; private set; }
        
        public void Init(View view)
        {
            View = view;
            ModelObserver = view.Model;
            OnInit();
        }
        
        public void Release()
        {
            OnRelease();
        }

        public void SetCommand(ICommand command)
        {
            OnCommandSet(command);
        }

        protected virtual void OnCommandSet(ICommand command){}
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