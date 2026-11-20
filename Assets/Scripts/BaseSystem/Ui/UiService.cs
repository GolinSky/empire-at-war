using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Ui.Base
{
    
    public class UiService : MonoBehaviour, IUiService, IHudVisibilityObserver
    {
        [SerializeField] private UiPlacement placement;
        private IAssetService _assetService;
        private bool _isFactionContent;
        private bool _isContentVisible;

        public bool IsHudVisible { get; private set; } = true;
        public IUiPlacement Placement => placement;

        [Inject]
        public void Constructor(IAssetService assetService) => _assetService = assetService;

        public TUi CreateUi<TUi>() where TUi : BaseUi
        {
            TUi prefab = _assetService.LoadComponent<TUi>(typeof(TUi).Name);
            return UnityEngine.Object.Instantiate(prefab, placement.GetParent<TUi>(), false);
        }

        public void SetContentLayout(bool isFaction, bool isGroup)
        {
            _isFactionContent = isFaction;
            placement.FactionLayout.SetLayout(true, false);
            placement.ShipLayout.SetLayout(false, isGroup);
            SetContentVisible(_isContentVisible);
        }

        public void SetContentVisible(bool isVisible)
        {
            _isContentVisible = isVisible;
            placement.FactionLayout.gameObject.SetActive(isVisible && _isFactionContent);
            UpdateShipLayoutVisibility();
        }

        // The ship panel hides through its own Canvas, not SetActive: reactivating it re-ran OnEnable on every
        // card and rebuilt all their meshes on each selection. HUD visibility is folded in so the nested canvas
        // never depends on how a disabled parent canvas treats it.
        private void UpdateShipLayoutVisibility()
        {
            bool isVisible = IsHudVisible && _isContentVisible && !_isFactionContent;
            placement.ShipLayoutCanvas.enabled = isVisible;
            placement.ShipLayoutRaycaster.enabled = isVisible;
        }

        public void SetHudVisible(bool isVisible)
        {
            IsHudVisible = isVisible;
            placement.DefaultCanvas.enabled = isVisible;
            placement.DynamicCanvas.enabled = isVisible;
            placement.FeedbackCanvas.enabled = isVisible;
            placement.ContextualSlot.gameObject.SetActive(isVisible);
            UpdateShipLayoutVisibility();
        }
    }
}
