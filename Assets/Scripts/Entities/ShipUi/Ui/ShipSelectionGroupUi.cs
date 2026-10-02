using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.Squadrons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public sealed class ShipSelectionGroupUi : MonoBehaviour
    {
        private const float HEADER_HEIGHT = 22f;
        private const float PADDING = 2f;
        private const float SPACING = 6f;
        private const float SHIP_HEADER_HEIGHT = 100f;

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private RectTransform groupRect;
        [SerializeField] private GridLayoutGroup entriesLayout;
        [SerializeField] private ShipUi entryPrefab;
        [SerializeField] private ShipAbilityBarUi abilityBar;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        private TooltipHoverView _tooltipHover;
        public void SetTooltipHover(TooltipHoverView hover)
        {
            _tooltipHover = hover;
            _tooltipHover.Register(tooltipTrigger);
            abilityBar.SetTooltipHover(hover);
        }

        private Action _onClicked;
        private int _shipCount;

        private void Awake() => button.onClick.AddListener(HandleClick);
        private void OnDestroy() => button.onClick.RemoveListener(HandleClick);

        public void Configure(ShipType shipType, Sprite icon, IReadOnlyList<ShipUiEntry> ships,
            IShipUiModelObserver model, Action<ShipType> onClicked, Action<ShipAbilityId> pressAbility)
        {
            Configure(shipType.ToString(), icon, ships, model, () => onClicked(shipType), pressAbility);
            tooltipTrigger.SetKey(shipType);
        }

        public void Configure(SquadronType squadronType, Sprite icon, IReadOnlyList<ShipUiEntry> squadrons,
            IShipUiModelObserver model, Action<SquadronType> onClicked, Action<ShipAbilityId> pressAbility)
        {
            Configure(squadronType.ToString(), icon, squadrons, model, () => onClicked(squadronType), pressAbility);
            tooltipTrigger.SetKey(squadronType);
        }

        private void Configure(string label, Sprite icon, IReadOnlyList<ShipUiEntry> ships,
            IShipUiModelObserver model, Action onClicked, Action<ShipAbilityId> pressAbility)
        {
            _onClicked = onClicked;
            _shipCount = ships.Count;
            typeLabel.text = $"{label}  ×{ships.Count}";

            List<ShipAbilitySlot> slots = new List<ShipAbilitySlot>();
            for (int i = 0; i < ships.Count; i++)
            {
                ShipUi entry = Instantiate(entryPrefab, entriesLayout.transform);
                entry.ConfigureEntry(icon, model, ships[i], HandleClick);
                entry.TooltipHover.ForwardTo(_tooltipHover, ships[i]);
                entry.gameObject.SetActive(true);
                for (int j = 0; j < ships[i].AbilitySlots.Count; j++)
                {
                    ShipAbilitySlot slot = ships[i].AbilitySlots[j];
                    slots.Add(slot);
                }
            }
            abilityBar.SetModel(model);
            abilityBar.SetSlots(slots, pressAbility);
        }

        public float GetWidth(float iconSize, float height)
        {
            int columns = GetColumns(iconSize, height);
            return Mathf.Max(columns * (iconSize + SPACING) - SPACING,
                196f) + PADDING * 2f;
        }

        public void SetLayout(float x, float iconSize, float height)
        {
            groupRect.anchoredPosition = new Vector2(x, 0f);
            groupRect.sizeDelta = new Vector2(GetWidth(iconSize, height), height);
            entriesLayout.cellSize = new Vector2(iconSize, iconSize + SHIP_HEADER_HEIGHT);
            entriesLayout.constraintCount = GetColumns(iconSize, height);
        }

        private int GetColumns(float iconSize, float height)
        {
            int rows = Mathf.Max(1, Mathf.FloorToInt(
                (height - HEADER_HEIGHT - PADDING + SPACING) /
                (iconSize + SHIP_HEADER_HEIGHT + SPACING)));
            return Mathf.CeilToInt(_shipCount / (float)rows);
        }

        private void HandleClick() => _onClicked();
    }
}
