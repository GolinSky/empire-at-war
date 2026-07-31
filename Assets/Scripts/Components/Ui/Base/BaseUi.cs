using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUi : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

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
            SetVisibility(true);
        }

        public virtual void Hide()
        {
            SetVisibility(false);
        }

        private void SetVisibility(bool isVisible)
        {
            if (canvasGroup == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires a bound {nameof(CanvasGroup)}.");
            }

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
            IsVisible = isVisible;
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
