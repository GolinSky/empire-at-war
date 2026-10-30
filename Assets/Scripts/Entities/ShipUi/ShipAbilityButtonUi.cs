using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipAbilityButtonUi : MonoBehaviour
    {
        private IReadOnlyList<ShipAbilitySlot> _slots;
        private readonly List<ShipAbilitySlot> _observedSlots = new List<ShipAbilitySlot>();

        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private GameObject targetingHighlight;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private TMP_Text cooldownText;
        private Action<ShipAbilityId> _onPressed;

        private bool _waiting;
        private int _displayedSeconds = -1;
        private bool _displayedWaiting;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public ShipAbilityId Id { get; private set; }

        private void Awake() => button.onClick.AddListener(HandleClick);

        private void OnEnable()
        {
            ObserveSlots();
            Render();
        }

        private void OnDisable() => UnobserveSlots();

        private void OnDestroy()
        {
            UnobserveSlots();
            button.onClick.RemoveListener(HandleClick);
        }

        public void Configure(ShipAbilityId id, IReadOnlyList<ShipAbilitySlot> slots,
            Sprite sprite, Action<ShipAbilityId> onPressed)
        {
            UnobserveSlots();
            Id = id;
            _slots = slots;
            tooltipTrigger.SetKey(slots);
            _onPressed = onPressed;
            icon.sprite = sprite;
            cooldownFill.sprite = sprite;
            if (isActiveAndEnabled) ObserveSlots();
            Render();
        }

        private void ObserveSlots()
        {
            if (_slots == null) return;
            foreach (ShipAbilitySlot slot in _slots)
            {
                slot.StateChanged += Render;
                _observedSlots.Add(slot);
            }
        }

        private void UnobserveSlots()
        {
            foreach (ShipAbilitySlot slot in _observedSlots) slot.StateChanged -= Render;
            _observedSlots.Clear();
        }

        public void SetWaiting(bool waiting) => _waiting = waiting;

        private void Update() => Render();

        private void Render()
        {
            if (_slots == null) return;
            float lowestProgress = 1f;
            float activeProgress = 1f;
            bool active = false;
            bool interactable = false;
            float remaining = float.MaxValue;
            float activeRemaining = float.MaxValue;
            bool allSuppressed = _slots.Count > 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                ShipAbilitySlot slot = _slots[i];
                lowestProgress = Mathf.Min(lowestProgress, slot.Progress01);
                remaining = Mathf.Min(remaining, slot.TimeLeft);
                active |= slot.State == ShipAbilityState.Active;
                if (slot.State == ShipAbilityState.Active)
                {
                    activeRemaining = Mathf.Min(activeRemaining, slot.TimeLeft);
                    activeProgress = Mathf.Min(activeProgress, slot.Progress01);
                }
                interactable |= slot.CanPress;
                allSuppressed &= slot.Owner.Modifiers.IsAbilitySuppressed;
            }
            cooldownFill.fillAmount = active ? activeProgress : lowestProgress;
            activeHighlight.SetActive(active);
            targetingHighlight.SetActive(_waiting);
            button.interactable = interactable;
            icon.color = allSuppressed ? new Color(0.45f, 0.45f, 0.55f, 1f) : Color.white;
            float displayedRemaining = active ? activeRemaining : remaining;
            int seconds = (active || !interactable) && displayedRemaining > 0f
                ? Mathf.CeilToInt(displayedRemaining) : 0;
            if (_displayedWaiting != _waiting || _displayedSeconds != seconds)
            {
                cooldownText.text = _waiting ? "TARGET" : seconds > 0 ? $"{seconds}s" : "";
                _displayedWaiting = _waiting;
                _displayedSeconds = seconds;
            }
        }

        private void HandleClick() => _onPressed(Id);
    }
}
