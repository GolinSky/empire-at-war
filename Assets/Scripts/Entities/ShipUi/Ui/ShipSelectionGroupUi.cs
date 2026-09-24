using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public sealed class ShipSelectionGroupUi : MonoBehaviour
    {
        private const float HEADER_HEIGHT = 70f;
        private const float PADDING = 8f;
        private const float SPACING = 6f;
        private const float SHIP_HEADER_HEIGHT = 36f;

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private RectTransform groupRect;
        [SerializeField] private GridLayoutGroup entriesLayout;
        [SerializeField] private ShipUi entryPrefab;
        [SerializeField] private ShipAbilityBarUi abilityBar;

        private ShipType _shipType;
        private Action<ShipType> _onClicked;
        private int _shipCount;
        private int _abilityCount;

        private void Awake() => button.onClick.AddListener(HandleClick);
        private void OnDestroy() => button.onClick.RemoveListener(HandleClick);

        public void Configure(ShipType shipType, Sprite icon, IReadOnlyList<ShipUiEntry> ships,
            IShipUiModelObserver model, Action<ShipType> onClicked, Action<ShipAbilityId> pressAbility)
        {
            _shipType = shipType;
            _onClicked = onClicked;
            _shipCount = ships.Count;
            typeLabel.text = $"{shipType}  ×{ships.Count}";

            List<ShipAbilitySlot> slots = new List<ShipAbilitySlot>();
            HashSet<ShipAbilityId> abilities = new HashSet<ShipAbilityId>();
            for (int i = 0; i < ships.Count; i++)
            {
                ShipUi entry = Instantiate(entryPrefab, entriesLayout.transform);
                entry.ConfigureEntry(icon, model, ships[i], HandleClick);
                entry.gameObject.SetActive(true);
                for (int j = 0; j < ships[i].AbilitySlots.Count; j++)
                {
                    ShipAbilitySlot slot = ships[i].AbilitySlots[j];
                    slots.Add(slot);
                    abilities.Add(slot.Id);
                }
            }
            _abilityCount = abilities.Count;
            abilityBar.SetModel(model);
            abilityBar.SetSlots(slots, pressAbility);
        }

        public float GetWidth(float iconSize, float height)
        {
            int columns = GetColumns(iconSize, height);
            return Mathf.Max(columns * (iconSize + SPACING) - SPACING,
                _abilityCount * 36f) + PADDING * 2f;
        }

        public void SetLayout(float x, float iconSize, float height)
        {
            groupRect.anchoredPosition = new Vector2(x, -PADDING);
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

        private void HandleClick() => _onClicked(_shipType);
    }
}
