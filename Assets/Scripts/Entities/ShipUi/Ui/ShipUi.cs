using EmpireAtWar.Models.ShipUi;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public class ShipUi : BaseUi, IShipUi
    {
        [SerializeField] private Image shipIconImage;
        [SerializeField] private Button disableSelectionButton;
        [SerializeField] private ShipAbilityBarUi abilityBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image shieldFill;

        private IShipUiModelObserver _model;
        private IHealthModelObserver _health;
        private IShipUiPresenter _presenter;
        private bool _isInitialized;
        private bool _isRouteActive = true;
        private bool _isEntry;
        private Action _onSelected;

        public void SetModel(IShipUiModelObserver model) => _model = model;
        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;
        public void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots) =>
            abilityBar.SetSlots(slots, _presenter.PressAbility);

        public void SetHealth(IHealthModelObserver health)
        {
            if (_health != null) _health.OnValueChanged -= UpdateHealth;
            _health = health;
            if (_health != null) _health.OnValueChanged += UpdateHealth;
            UpdateHealth();
        }

        public void Initialize()
        {
            _model.OnSelectionChanged += UpdateVisibility;
            abilityBar.SetModel(_model);
            _onSelected = _presenter.CloseSelection;
            disableSelectionButton.onClick.AddListener(HandleSelection);
            _isInitialized = true;
            UpdateVisibility();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            SetHealth(null);
            if (!_isEntry) _model.OnSelectionChanged -= UpdateVisibility;
            disableSelectionButton.onClick.RemoveListener(HandleSelection);
            _isInitialized = false;
        }

        public void ConfigureEntry(Sprite icon, IShipUiModelObserver model,
            ShipUiEntry entry, Action onSelected)
        {
            _isEntry = true;
            _model = model;
            _onSelected = onSelected;
            shipIconImage.sprite = icon;
            shipIconImage.enabled = icon != null;
            abilityBar.SetModel(model);
            abilityBar.SetSlots(entry.AbilitySlots, entry.PressAbility);
            SetHealth(entry.Health);
            disableSelectionButton.onClick.AddListener(HandleSelection);
            _isInitialized = true;
        }

        private void HandleSelection() => _onSelected();

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
            gameObject.SetActive(isVisible);
            Sprite icon = null;
            if (isVisible)
            {
                if (_model.SelectedShipType.HasValue)
                    icon = _model.GetShipIcon(_model.SelectedShipType.Value);
                else if (_model.SelectedSquadronType.HasValue)
                    icon = _model.GetSquadronIcon(_model.SelectedSquadronType.Value);
            }
            shipIconImage.sprite = icon;
            shipIconImage.enabled = icon != null;
        }
    }
}
