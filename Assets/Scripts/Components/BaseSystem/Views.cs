using UnityEngine;

namespace EmpireAtWar.Mvc
{
    public interface IView
    {
        ViewComponent[] ViewComponents { get; }
    }

    public abstract class BaseView : MonoBehaviour, IView
    {
        [SerializeField] protected ViewComponent[] viewComponents;

        public Transform Transform => transform;
        public ViewComponent[] ViewComponents => viewComponents;
    }

    public abstract class ViewComponent : MonoBehaviour
    {
        public virtual IModelObserver ModelObserver { get; protected set; }
        public virtual BaseView View { get; protected set; }

        public void SetView(BaseView view)
        {
            View = view;
        }

        public void SetModelObserver(IModelObserver modelObserver)
        {
            ModelObserver = modelObserver;
        }

        public virtual void Init()
        {
            OnInit();
        }

        public virtual void Dispose()
        {
            OnRelease();
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnRelease()
        {
        }
    }
}
