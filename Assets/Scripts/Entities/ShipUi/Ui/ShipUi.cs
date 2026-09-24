using EmpireAtWar.Models.ShipUi;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
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

        private IShipUiModelObserver _model;
        private IShipUiPresenter _presenter;
        private bool _isInitialized;
        private bool _isRouteActive = true;
        private bool _isEntry;
        private Action _onSelected;

        public void SetModel(IShipUiModelObserver model) => _model = model;
        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;
        public void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots) =>
            abilityBar.SetSlots(slots, _presenter.PressAbility);

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
            disableSelectionButton.onClick.AddListener(HandleSelection);
            _isInitialized = true;
        }

        private void HandleSelection() => _onSelected();

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
            if (isVisible && _model.SelectedShipType.HasValue)
            {
                shipIconImage.sprite = _model.GetShipIcon(_model.SelectedShipType.Value);
            }
            shipIconImage.enabled = isVisible && _model.SelectedShipType.HasValue &&
                                    shipIconImage.sprite != null;
        }
    }
}
