using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Ui.Base
{
    
    public class UiService : MonoBehaviour, IUiService, IHudVisibilityObserver
    {
        [SerializeField] private UiPlacement placement;
        private EmpireAtWar.Mvc.IAssetService _assetService;
        private bool _isFactionContent;
        private bool _isContentVisible;

        public bool IsHudVisible { get; private set; } = true;
        public IUiPlacement Placement => placement;

        [Inject]
        public void Constructor(EmpireAtWar.Mvc.IAssetService assetService) => _assetService = assetService;

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
            placement.ShipLayout.gameObject.SetActive(isVisible && !_isFactionContent);
        }

        public void SetHudVisible(bool isVisible)
        {
            IsHudVisible = isVisible;
            placement.DefaultCanvas.enabled = isVisible;
            placement.DynamicCanvas.enabled = isVisible;
            placement.FeedbackCanvas.enabled = isVisible;
            placement.ContextualSlot.gameObject.SetActive(isVisible);
        }
    }
}
