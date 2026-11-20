using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipAbilityBarUi : MonoBehaviour
    {
        private IShipUiModelObserver _model;

        [SerializeField] private ShipAbilityButtonUi buttonPrefab;
        [SerializeField] private Transform buttonParent;
        [SerializeField] private ShipAutoPowerButtonUi autoPowerButton;
        [Tooltip("The bar's AbilityBar anchor; it also marks the first ability button for tutorial hints.")]
        [SerializeField] private TutorialAnchor tutorialAnchor;
        [Tooltip("Scrolls the buttons only while they overflow the bar.")]
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("Clips the buttons only while they overflow the bar.")]
        [SerializeField] private RectMask2D viewportMask;
        private readonly List<ShipAbilityButtonUi> _buttons = new List<ShipAbilityButtonUi>();
        private readonly Dictionary<ShipAbilityId, List<ShipAbilitySlot>> _groups =
            new Dictionary<ShipAbilityId, List<ShipAbilitySlot>>();
        private TooltipHoverView _tooltipHover;

        public void SetTooltipHover(TooltipHoverView hover) => _tooltipHover = hover;

        public void SetModel(IShipUiModelObserver model) => _model = model;

        public void SetAutoPower(ShipAutoPowerUiState state, Action onPressed)
        {
            _tooltipHover.Register(autoPowerButton.TooltipTrigger);
            autoPowerButton.Configure(state, onPressed);
        }

        public void SetSlots(IReadOnlyList<ShipAbilitySlot> slots, Action<ShipAbilityId> onPressed)
        {
            foreach (List<ShipAbilitySlot> group in _groups.Values) group.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                ShipAbilitySlot slot = slots[i];
                if (!_groups.TryGetValue(slot.Id, out List<ShipAbilitySlot> group))
                {
                    group = new List<ShipAbilitySlot>();
                    _groups.Add(slot.Id, group);
                }
                group.Add(slot);
            }

            int buttonIndex = 0;
            foreach (KeyValuePair<ShipAbilityId, List<ShipAbilitySlot>> group in _groups)
            {
                if (group.Value.Count == 0) continue;
                if (buttonIndex == _buttons.Count)
                    _buttons.Add(Instantiate(buttonPrefab, buttonParent));
                _buttons[buttonIndex].Configure(group.Key, group.Value,
                    group.Value[0].Definition.Icon, onPressed);
                if (_tooltipHover != null) _tooltipHover.Register(_buttons[buttonIndex].TooltipTrigger);
                _buttons[buttonIndex].gameObject.SetActive(true);
                buttonIndex++;
            }
            for (int i = buttonIndex; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
            if (buttonIndex > 0)
                tutorialAnchor.Mark(TutorialAnchorId.AbilityButton,
                    (RectTransform)_buttons[0].transform, _buttons[0].TutorialHighlight);
            else tutorialAnchor.Unmark(TutorialAnchorId.AbilityButton);
        }

        private void Update()
        {
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i].gameObject.activeSelf)
                    _buttons[i].SetWaiting(IsWaiting(_buttons[i].Id));
        }

        // An enabled RectMask2D re-clips every child each frame and splits UI batches, and one bar exists per
        // selected unit type; clip and scroll only while the buttons do not fit.
        private void LateUpdate()
        {
            bool overflows = scrollRect.content.rect.width > scrollRect.viewport.rect.width;
            if (viewportMask.enabled == overflows) return;
            scrollRect.StopMovement();
            viewportMask.enabled = overflows;
            scrollRect.enabled = overflows;
        }

        private bool IsWaiting(ShipAbilityId id)
        {
            if (_model.PendingAbilityId != id) return false;
            foreach (ShipAbilitySlot slot in _groups[id])
                if (_model.IsPendingCaster(slot.Owner.Entity)) return true;
            return false;
        }
    }
}
