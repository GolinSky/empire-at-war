using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.UnitActions.Ui
{
    public sealed class UnitActionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject pendingHighlight;
        [SerializeField] private CanvasGroup contentGroup;

        [SerializeField] private UnitActionId actionId;

        public event Action<UnitActionId> Pressed;

        public UnitActionId ActionId => actionId;

        public void Initialize() => button.onClick.AddListener(HandleClick);

        public void Dispose() => button.onClick.RemoveListener(HandleClick);

        public void SetAvailable(bool available)
        {
            button.interactable = available;
            contentGroup.alpha = available ? 1f : 0.3f;
        }

        public void SetPending(bool pending) => pendingHighlight.SetActive(pending);

        private void HandleClick() => Pressed?.Invoke(actionId);
    }
}
