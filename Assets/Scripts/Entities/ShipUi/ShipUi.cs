using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Models.ShipUi;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ShipUi
{
    public class ShipUi : BaseUi
    {
        private IShipUiModelObserver _model;
        private IShipIconProvider _icons;
        private IHealthModelObserver _health;
        private IShipUiPresenter _presenter;

        [SerializeField] private Image shipIconImage;
        [SerializeField] private Button disableSelectionButton;
        [SerializeField] private Button focusButton;
        [SerializeField] private ShipAbilityBarUi abilityBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image shieldFill;
        [SerializeField] private TooltipHoverView tooltipHover;
        private Action _onSelected;
        private Action _onFocused;

        private bool _isInitialized;
        private bool _isRouteActive = true;
        private bool _isEntry;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void SetIcons(IShipIconProvider icons) => _icons = icons;

        public void Initialize()
        {
            _model.OnSelectionChanged += UpdateVisibility;
            abilityBar.SetModel(_model);
            disableSelectionButton.enabled = false;
            focusButton.enabled = false;
            _isInitialized = true;
            UpdateVisibility();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            SetHealth(null);
            if (!_isEntry) _model.OnSelectionChanged -= UpdateVisibility;
            disableSelectionButton.onClick.RemoveListener(HandleSelection);
            focusButton.onClick.RemoveListener(HandleFocus);
            _onSelected = null;
            _onFocused = null;
            _isInitialized = false;
        }

        public void SetModel(IShipUiModelObserver model) => _model = model;

        public void AssignPresenter(IShipUiPresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("ShipUi presenter can only be assigned once.");
            _presenter = presenter;
        }

        public void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots)
        {
            abilityBar.SetTooltipHover(tooltipHover);
            abilityBar.SetSlots(slots, _presenter.PressAbility);
        }

        public void SetHealth(IHealthModelObserver health)
        {
            if (_health != null) _health.OnValueChanged -= UpdateHealth;
            _health = health;
            if (_health != null) _health.OnValueChanged += UpdateHealth;
            UpdateHealth();
        }

        public void ConfigureEntry(Sprite icon, ShipUiEntry entry, Action onSelected)
        {
            Dispose();
            _isEntry = true;
            _onSelected = onSelected;
            _onFocused = entry.Focus;
            shipIconImage.sprite = icon;
            shipIconImage.enabled = icon != null;
            abilityBar.gameObject.SetActive(false);
            SetHealth(entry.Health);
            disableSelectionButton.onClick.AddListener(HandleSelection);
            focusButton.onClick.AddListener(HandleFocus);
            _isInitialized = true;
        }

        private void HandleSelection() => _onSelected();

        private void HandleFocus() => _onFocused();

        private void UpdateHealth()
        {
            healthFill.fillAmount = _health != null ? _health.HullPercentage : 0f;
            shieldFill.fillAmount = _health != null ? _health.ShieldPercentage : 0f;
        }

        private void OnDestroy() => Dispose();

        public override void Show()
        {
            _isRouteActive = true;
            base.Show();
            UpdateVisibility();
        }

        public override void Hide()
        {
            _isRouteActive = false;
            base.Hide();
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool isVisible = _isRouteActive && _model.HasShips;
            if (isVisible) base.Show();
            else base.Hide();
            Sprite icon = null;
            if (isVisible)
            {
                if (_model.SelectedShipType.HasValue)
                    icon = _icons.GetShipIcon(_model.SelectedShipType.Value);
                else if (_model.SelectedSquadronType.HasValue)
                    icon = _icons.GetSquadronIcon(_model.SelectedSquadronType.Value);
            }
            shipIconImage.sprite = icon;
            shipIconImage.enabled = icon != null;
        }
    }
}
