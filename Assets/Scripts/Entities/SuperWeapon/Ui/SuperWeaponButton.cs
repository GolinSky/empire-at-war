using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Button rowButton;
        [SerializeField] private GameObject pendingHighlight;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Image buildProgress;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private MPUIKIT.MPImage background;

        [SerializeField] private SuperWeaponType weaponType;
        private bool _pending;

        private static readonly Color32 Gold = new(210, 169, 94, 255);
        private static readonly Color32 Blue = new(109, 184, 226, 255);
        private static readonly Color32 Warning = new(232, 110, 108, 255);
        private static readonly Color32 Slate = new(32, 46, 64, 255);
        private static readonly Color32 Outline = new(52, 67, 87, 255);
        private static readonly Color32 LightText = new(242, 240, 230, 255);
        private static readonly Color32 DarkText = new(16, 26, 41, 255);

        public event Action<SuperWeaponType> Pressed;
        public event Action<SuperWeaponType> PurchaseRequested;

        public SuperWeaponType WeaponType => weaponType;

        public void Initialize()
        {
            button.onClick.AddListener(HandlePurchase);
            rowButton.onClick.AddListener(HandleClick);
        }

        public void Dispose()
        {
            button.onClick.RemoveListener(HandlePurchase);
            rowButton.onClick.RemoveListener(HandleClick);
        }

        public void Render(SuperWeaponState state, bool canAct, string detail, float progress)
        {
            bool charging = state == SuperWeaponState.Charging;
            bool ready = state == SuperWeaponState.Ready && canAct;
            bool showPurchase = state == SuperWeaponState.Unavailable;
            button.gameObject.SetActive(showPurchase);
            button.interactable = showPurchase && canAct;
            rowButton.interactable = !showPurchase && canAct;
            contentGroup.alpha = 1f;
            pendingHighlight.SetActive(_pending);
            stateText.text = _pending ? "Targeting" : detail;
            stateText.color = _pending || charging ? Gold : canAct ? Blue : Warning;
            background.OutlineColor = _pending || charging ? Gold : ready ? Blue : Outline;
            buildProgress.enabled = charging;
            buildProgress.fillAmount = progress;
            actionText.color = canAct ? DarkText : LightText;
            button.targetGraphic.color = canAct ? Gold : Slate;
        }

        public void SetPending(bool pending)
        {
            _pending = pending;
            pendingHighlight.SetActive(pending);
        }

        private void HandleClick() => Pressed?.Invoke(weaponType);

        private void HandlePurchase() => PurchaseRequested?.Invoke(weaponType);
    }
}
