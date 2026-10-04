using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject pendingHighlight;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private MPUIKIT.MPImage background;

        [SerializeField] private SuperWeaponType weaponType;
        private SuperWeaponState _state;

        private bool _pending;

        public event Action<SuperWeaponType> Pressed;

        public SuperWeaponType WeaponType => weaponType;

        public void Initialize() => button.onClick.AddListener(HandleClick);

        public void Dispose() => button.onClick.RemoveListener(HandleClick);

        public void SetState(SuperWeaponState state)
        {
            _state = state;
            button.interactable = state == SuperWeaponState.Ready;
            contentGroup.alpha = state == SuperWeaponState.Unavailable ? 0.35f : 1f;
            RenderState();
        }

        public void SetPending(bool pending)
        {
            _pending = pending;
            pendingHighlight.SetActive(pending);
            RenderState();
        }

        public void SetRemaining(float seconds)
        {
            if ((_state == SuperWeaponState.Charging || _state == SuperWeaponState.Cooldown) && !_pending)
                stateText.text = $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
        }

        private void RenderState()
        {
            stateText.text = _pending ? "TARGETING" : _state.ToString().ToUpperInvariant();
            stateText.color = _pending || _state == SuperWeaponState.Charging
                ? new Color32(243, 182, 77, 255)
                : _state == SuperWeaponState.Cooldown ? new Color32(214, 92, 76, 255)
                : new Color32(54, 200, 243, 255);
            background.OutlineColor = _pending ? new Color32(243, 182, 77, 255)
                : _state == SuperWeaponState.Ready ? new Color32(54, 200, 243, 255)
                : new Color32(40, 70, 87, 255);
        }

        private void HandleClick() => Pressed?.Invoke(weaponType);
    }
}
