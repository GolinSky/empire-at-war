using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public abstract class BaseUi : MonoBehaviour
    {
        public bool IsVisible { get; private set; } = true;

        public virtual void SetParent(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            transform.SetParent(parent, false);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);// use canvas group
            IsVisible = true;
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            IsVisible = false;
        }
    }

    public abstract class BaseUi<TModel> : BaseUi
        where TModel : IModelObserver
    {
        [Inject]
        public TModel Model { get; }
    }

    public abstract class BaseUi<TModel, TCommand> : BaseUi<TModel>
        where TModel : IModelObserver
        where TCommand : ICommand
    {
        [Inject] protected TCommand Command { get; }
    }
}
