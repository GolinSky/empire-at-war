using EmpireAtWar.Commands.PopupCommands;
using EmpireAtWar.Services.Popup;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    public abstract class PopupUi : MonoBehaviour, IInitializable, ILateDisposable
    {
        [SerializeField] private PopupType popupType; // not need here
        [SerializeField] protected Button closeButton;

        [Inject] private IPopupCommand popupCommand;

        public void OpenPopup()
        {
            SetPopupState(true);
            OnPopupOpen();
        }

        public void ClosePopup()
        {
            popupCommand.ClosePopup(popupType);
            SetPopupState(false);
            OnPopupClose();
        }

        public void SetParent(Transform parent)
        {
            transform.SetParent(parent, false);
            transform.localScale = Vector3.one;
        }

        private void SetPopupState(bool state)
        {
            gameObject.SetActive(state);
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