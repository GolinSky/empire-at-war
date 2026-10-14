using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Ui.Base;
using MPUIKIT;
using Utilities.ScriptUtils.EditorSerialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Entities.CoreGameUi
{
    public class CoreGameUi : BaseUi
    {
        private ICoreGamePresenter _presenter;

        [SerializeField] private Button timeButton;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Button reinforcementButton;
        [SerializeField] private Button videoModeButton;
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        [SerializeField] private MPImage panelImage;
        [SerializeField] private DictionaryWrapper<bool, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameSpeed, Sprite> speedUpSprites;
        [SerializeField] private UnitActionsView unitActionsView;
        [SerializeField] private SuperWeaponsView superWeaponsView;
        [SerializeField] private GameObject contextualPopup;
        [SerializeField] private TMP_Text factionText;
        [SerializeField] private TMP_Text contentTitle;
        [SerializeField] private GameObject productionLabel;
        [SerializeField] private GameObject selectionSlots;
        [SerializeField] private Button clearFleetButton;
        [SerializeField] private CanvasGroup battleControls;

        private bool _isInitialized;
        private bool _hasContentLayout;
        private bool _isFactionLayout;
        private bool _isShipGroupLayout;
        private (string faction, bool ended, bool factionLayout, bool visible)? _lastHudStatus;

        public TooltipHoverView TooltipHover => tooltipHover;

        public IUnitActionsView UnitActionsView => unitActionsView;
        public ISuperWeaponsView SuperWeaponsView => superWeaponsView;
        public Transform ContextualPopup => contextualPopup.transform;

        public void Initialize()
        {
            timeButton.onClick.AddListener(_presenter.Play);
            speedUpButton.onClick.AddListener(_presenter.SpeedUp);
            reinforcementButton.onClick.AddListener(_presenter.ToggleReinforcement);
            videoModeButton.onClick.AddListener(_presenter.StartCinematic);
            clearFleetButton.onClick.AddListener(_presenter.ClearFleetSelection);
            // Hidden until a super weapon presenter binds; battles without super weapons have none.
            superWeaponsView.SetAvailable(false);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            timeButton.onClick.RemoveListener(_presenter.Play);
            speedUpButton.onClick.RemoveListener(_presenter.SpeedUp);
            reinforcementButton.onClick.RemoveListener(_presenter.ToggleReinforcement);
            videoModeButton.onClick.RemoveListener(_presenter.StartCinematic);
            clearFleetButton.onClick.RemoveListener(_presenter.ClearFleetSelection);
            _isInitialized = false;
        }

        public void SetHudStatus(string faction, int level, int selectionCount, bool battleEnded)
        {
            var status = (faction, battleEnded, _isFactionLayout, IsVisible);
            if (_lastHudStatus == status) return;
            _lastHudStatus = status;
            factionText.text = faction.ToUpperInvariant();
            contentTitle.text = _isFactionLayout ? $"{faction.ToUpperInvariant()} STARBASE" : "FLEET SELECTION";
            timeButton.interactable = !battleEnded;
            speedUpButton.interactable = !battleEnded;
            reinforcementButton.interactable = !battleEnded;
            videoModeButton.interactable = !battleEnded;
            clearFleetButton.interactable = !battleEnded;
            battleControls.interactable = !battleEnded && IsVisible;
            battleControls.blocksRaycasts = !battleEnded && IsVisible;
        }

        public void SetTimeControls(bool isPaused, GameSpeed speed)
        {
            timeImage.sprite = timeSprites.Dictionary[isPaused];
            speedUpImage.sprite = speedUpSprites.Dictionary[speed];
        }

        public void AssignPresenter(ICoreGamePresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("CoreGameUi presenter can only be assigned once.");
            _presenter = presenter;
        }

        private void OnDestroy()
        {
            Dispose();
            Destroy(contextualPopup);
        }

        public void SetContentVisible(bool isVisible)
        {
            panelImage.gameObject.SetActive(isVisible);
        }

        public void SetContentLayout(bool isFactionSelection, bool isShipGroupSelection)
        {
            if (_hasContentLayout && _isFactionLayout == isFactionSelection &&
                _isShipGroupLayout == isShipGroupSelection) return;
            _hasContentLayout = true;
            _isFactionLayout = isFactionSelection;
            _isShipGroupLayout = isShipGroupSelection;
            bool hasHeader = isFactionSelection;
            panelImage.rectTransform.anchorMax = new Vector2(1f, 0f);
            panelImage.rectTransform.sizeDelta = new Vector2(-612f, hasHeader ? 274f : 222f);
            contentTitle.gameObject.SetActive(hasHeader);
            productionLabel.SetActive(isFactionSelection);
            selectionSlots.SetActive(!hasHeader);
            clearFleetButton.gameObject.SetActive(isShipGroupSelection);
        }

    }
}
