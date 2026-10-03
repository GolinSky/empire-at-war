using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Views
{
    public sealed class ShipAbilityButtonUi : MonoBehaviour
    {
        private IReadOnlyList<ShipAbilitySlot> _slots;

        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private GameObject targetingHighlight;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private TMP_Text cooldownText;
        private Action<ShipAbilityId> _onPressed;

        private bool _waiting;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public ShipAbilityId Id { get; private set; }

        private void Awake() => button.onClick.AddListener(HandleClick);

        private void OnDestroy() => button.onClick.RemoveListener(HandleClick);

        public void Configure(ShipAbilityId id, IReadOnlyList<ShipAbilitySlot> slots,
            Sprite sprite, Action<ShipAbilityId> onPressed)
        {
            Id = id;
            _slots = slots;
            tooltipTrigger.SetKey(slots);
            _onPressed = onPressed;
            icon.sprite = sprite;
            cooldownFill.sprite = sprite;
        }

        public void SetWaiting(bool waiting) => _waiting = waiting;

        private void Update()
        {
            if (_slots == null) return;
            float lowestProgress = 1f;
            bool active = false;
            bool interactable = false;
            float remaining = float.MaxValue;
            for (int i = 0; i < _slots.Count; i++)
            {
                ShipAbilitySlot slot = _slots[i];
                lowestProgress = Mathf.Min(lowestProgress, slot.Progress01);
                remaining = Mathf.Min(remaining, slot.TimeLeft);
                active |= slot.State == ShipAbilityState.Active;
                interactable |= slot.State == ShipAbilityState.Ready ||
                    slot.State == ShipAbilityState.Active && slot.Definition.CanCancel;
            }
            cooldownFill.fillAmount = lowestProgress;
            activeHighlight.SetActive(active);
            targetingHighlight.SetActive(_waiting);
            button.interactable = interactable;
            cooldownText.text = _waiting ? "TARGET" : !interactable && remaining > 0f
                ? $"{Mathf.CeilToInt(remaining)}s" : "";
        }

        private void HandleClick() => _onPressed(Id);
    }
}
