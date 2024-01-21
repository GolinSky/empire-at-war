using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.ViewComponents;

namespace WorkShop.LightWeightFramework
{
    public abstract class View : MonoBehaviour, IView
    {
        [SerializeField] protected ViewComponent[] viewComponents;
        public IModelObserver ModelObserver { get; private set; }

        public virtual void Init(IModelObserver model)
        {
            ModelObserver = model;
            for (var i = 0; i < viewComponents.Length; i++)
            {
                InitViewComponent(viewComponents[i]);
            }
        }

        public virtual void Release()
        {
            for (var i = 0; i < viewComponents.Length; i++)
            {
                ReleaseViewComponent(viewComponents[i]);
            }
        }

        private void InitViewComponent(ViewComponent viewComponent)
        {
            viewComponent.Init(this);
            OnInitViewComponent(viewComponent);
        }

        private void ReleaseViewComponent(ViewComponent viewComponent)
        {
            viewComponent.Release();
            OnReleaseViewComponent(viewComponent);
        }

        protected virtual void OnInitViewComponent(ViewComponent viewComponent){}
        protected virtual void OnReleaseViewComponent(ViewComponent viewComponent){}
    }
}