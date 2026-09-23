using EmpireAtWar.Models.ShipUi;
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

        private IShipUiModelObserver _model;
        private IShipUiPresenter _presenter;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public void SetModel(IShipUiModelObserver model) => _model = model;
        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            _model.OnSelectionChanged += UpdateVisibility;
            disableSelectionButton.onClick.AddListener(_presenter.CloseSelection);
            _isInitialized = true;
            UpdateVisibility();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _model.OnSelectionChanged -= UpdateVisibility;
            disableSelectionButton.onClick.RemoveListener(_presenter.CloseSelection);
            _isInitialized = false;
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
            if (isVisible && _model.SelectedShipType.HasValue)
            {
                shipIconImage.sprite = _model.GetShipIcon(_model.SelectedShipType.Value);
            }
            shipIconImage.enabled = isVisible && _model.SelectedShipType.HasValue &&
                                    shipIconImage.sprite != null;
        }
    }
}
