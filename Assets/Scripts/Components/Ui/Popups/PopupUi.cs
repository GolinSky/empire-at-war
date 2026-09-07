using EmpireAtWar.Commands.PopupCommands;
using EmpireAtWar.Services.Popup;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupUi : MonoBehaviour, IInitializable, ILateDisposable
    {
        [SerializeField] private PopupType popupType; // not need here
        [SerializeField] protected Button closeButton;
        [SerializeField] private CanvasGroup canvasGroup;

        [Inject] private IPopupCommand _popupCommand;

        public void OpenPopup()
        {
            SetPopupState(true);
            OnPopupOpen();
        }

        private void ClosePopup()
        {
            _popupCommand.ClosePopup(popupType);
            SetPopupState(false);
            OnPopupClose();
        }

        private void SetPopupState(bool state)
        {
            if (canvasGroup == null)
            {
                throw new System.InvalidOperationException(
                    $"{GetType().Name} requires a bound {nameof(CanvasGroup)}.");
            }

            canvasGroup.alpha = state ? 1f : 0f;
            canvasGroup.interactable = state;
            canvasGroup.blocksRaycasts = state;
        }

        protected virtual void OnPopupOpen()
        {
        }

        protected virtual void OnPopupClose()
        {
        }

        public virtual void Initialize() //todo: make template method
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        public virtual void LateDispose() //todo: make template method
        {
            closeButton.onClick.RemoveListener(ClosePopup);
        }
    }
}
