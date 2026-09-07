using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views
{
    public class ShipUi : BaseUi<IShipUiModelObserver, IShipUiCommand>,
        IInitializable, ILateDisposable
    {
        [SerializeField] private Image shipIconImage;
        [SerializeField] private Button disableSelectionButton;

        private bool _hasMovableSelection;
        private bool _isRouteActive = true;

        public void Initialize()
        {
            Model.OnSelectionChanged += HandleChangedSelection;
            disableSelectionButton.onClick.AddListener(CloseSelection);
        }
    
        public void LateDispose()
        {
            Model.OnSelectionChanged -= HandleChangedSelection;
            disableSelectionButton.onClick.RemoveListener(CloseSelection);
        }
        
        private void CloseSelection()
        {
            Command.CloseSelection();
        }
        
        private void HandleChangedSelection(bool hasMovableSelection)
        {
            _hasMovableSelection = hasMovableSelection;
            UpdateVisibility();
        }

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
        }

        private void UpdateVisibility()
        {
            bool isVisible = _isRouteActive && _hasMovableSelection;
            gameObject.SetActive(isVisible);
            shipIconImage.enabled = isVisible && Model.ShipIcon != null;
            if (shipIconImage.enabled)
            {
                shipIconImage.sprite = Model.ShipIcon;
            }
        }
    }
}
