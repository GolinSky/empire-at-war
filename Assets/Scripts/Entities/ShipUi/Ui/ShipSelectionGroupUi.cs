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
        private const int MAX_INDIVIDUAL_SHIPS = 4;
        private const float HEADER_HEIGHT = 32f;
        private const float COLUMN_SPACING = 8f;
        private const float ROW_SPACING = 2f;

        [SerializeField] private Button button;
        [SerializeField] private RectTransform groupRect;
        [SerializeField] private GridLayoutGroup entriesLayout;
        [SerializeField] private ShipUi entryPrefab;
        [SerializeField] private ShipAbilityBarUi abilityBar;
        [SerializeField] private RectTransform abilityBarRect;
        [SerializeField] private GameObject summaryCard;
        [SerializeField] private Image summaryIcon;
        [SerializeField] private TMP_Text summaryCount;
        [SerializeField] private Image summaryHealthFill;
        [SerializeField] private Image summaryShieldFill;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        private TooltipHoverView _tooltipHover;
        private Action _onClicked;
        private IReadOnlyList<ShipUiEntry> _ships;
        private int _shipCount;

        public void SetTooltipHover(TooltipHoverView hover)
        {
            _tooltipHover = hover;
            _tooltipHover.Register(tooltipTrigger);
            abilityBar.SetTooltipHover(hover);
        }

        private void Awake() => button.onClick.AddListener(HandleClick);

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClick);
            if (_shipCount > MAX_INDIVIDUAL_SHIPS)
                foreach (ShipUiEntry ship in _ships)
                    ship.Health.OnValueChanged -= UpdateSummaryHealth;
        }

        public void Configure(ShipType shipType, Sprite icon, IReadOnlyList<ShipUiEntry> ships,
            IShipUiModelObserver model, Action<ShipType> onClicked, Action<ShipAbilityId> pressAbility)
        {
            Configure(icon, ships, model, () => onClicked(shipType), pressAbility);
            tooltipTrigger.SetKey(shipType);
        }

        public void Configure(SquadronType squadronType, Sprite icon, IReadOnlyList<ShipUiEntry> squadrons,
            IShipUiModelObserver model, Action<SquadronType> onClicked, Action<ShipAbilityId> pressAbility)
        {
            Configure(icon, squadrons, model, () => onClicked(squadronType), pressAbility);
            tooltipTrigger.SetKey(squadronType);
        }

        private void Configure(Sprite icon, IReadOnlyList<ShipUiEntry> ships,
            IShipUiModelObserver model, Action onClicked, Action<ShipAbilityId> pressAbility)
        {
            _onClicked = onClicked;
            _ships = ships;
            _shipCount = ships.Count;
            bool collapsed = _shipCount > MAX_INDIVIDUAL_SHIPS;
            summaryCard.SetActive(collapsed);
            entriesLayout.gameObject.SetActive(!collapsed);

            List<ShipAbilitySlot> slots = new List<ShipAbilitySlot>();
            foreach (ShipUiEntry ship in ships)
            {
                if (collapsed)
                    ship.Health.OnValueChanged += UpdateSummaryHealth;
                else
                {
                    ShipUi entry = Instantiate(entryPrefab, entriesLayout.transform);
                    entry.ConfigureEntry(icon, ship, HandleClick);
                    entry.TooltipHover.ForwardTo(_tooltipHover, ship);
                    entry.gameObject.SetActive(true);
                }
                slots.AddRange(ship.AbilitySlots);
            }
            if (collapsed)
            {
                summaryIcon.sprite = icon;
                summaryCount.text = $"×{_shipCount}";
                UpdateSummaryHealth();
            }
            abilityBar.SetModel(model);
            abilityBar.SetSlots(slots, pressAbility);
        }

        private void UpdateSummaryHealth()
        {
            float hull = 0f;
            float shields = 0f;
            foreach (ShipUiEntry ship in _ships)
            {
                hull += ship.Health.HullPercentage;
                shields += ship.Health.ShieldPercentage;
            }
            summaryHealthFill.fillAmount = hull / _shipCount;
            summaryShieldFill.fillAmount = shields / _shipCount;
        }

        public float GetWidth(float cardWidth)
        {
            int columns = _shipCount == 3 || _shipCount == 4 ? 2 : 1;
            return columns * cardWidth + (columns - 1) * COLUMN_SPACING;
        }

        public void SetLayout(float x, float cardWidth, float height)
        {
            float width = GetWidth(cardWidth);
            groupRect.anchoredPosition = new Vector2(x, 0f);
            groupRect.sizeDelta = new Vector2(width, height);
            abilityBarRect.sizeDelta = new Vector2((width - 10f) / abilityBarRect.localScale.x, 40f);
            int rows = _shipCount > 1 && _shipCount <= MAX_INDIVIDUAL_SHIPS ? 2 : 1;
            entriesLayout.cellSize = new Vector2(cardWidth,
                (height - HEADER_HEIGHT - (rows - 1) * ROW_SPACING) / rows);
            entriesLayout.spacing = new Vector2(COLUMN_SPACING, ROW_SPACING);
            entriesLayout.startAxis = GridLayoutGroup.Axis.Vertical;
            entriesLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            entriesLayout.constraintCount = rows;
            entriesLayout.childAlignment = TextAnchor.UpperLeft;
        }

        private void HandleClick() => _onClicked();
    }
}
