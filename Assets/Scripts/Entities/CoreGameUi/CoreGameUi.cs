using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
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
        [SerializeField] private GameObject reinforcementReadyIndicator;
        [SerializeField] private Button videoModeButton;
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        [SerializeField] private MPImage panelImage;
        [SerializeField] private DictionaryWrapper<bool, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameSpeed, Sprite> speedUpSprites;
        [SerializeField] private UnitActionsView unitActionsView;
        [SerializeField] private SuperWeaponsView superWeaponsView;
        [SerializeField] private GameObject contextualPopup;
        [SerializeField] private GameObject selectionSlots;
        [SerializeField] private Button clearFleetButton;
        [SerializeField] private CanvasGroup battleControls;
        [SerializeField] private TMP_Text populationText;
        [SerializeField] private GameObject populationDisplay;
        [SerializeField] private GameObject battleDurationPanel;
        [SerializeField] private TMP_Text battleDurationText;

        private bool _isInitialized;
        private bool _hasContentLayout;
        private bool _isFactionLayout;
        private bool _isShipGroupLayout;
        private bool _isContentVisible;
        private bool _isSpectator;
        private bool _hasTimeControls = true;
        private (bool ended, bool visible)? _lastHudStatus;
        private int _displayedDurationSeconds = -1;

        public TooltipHoverView TooltipHover => tooltipHover;

        public IUnitActionsView UnitActionsView => unitActionsView;
        public ISuperWeaponsView SuperWeaponsView => superWeaponsView;
        public Transform ContextualPopup => contextualPopup.transform;

        public void Initialize()
        {
            populationDisplay.SetActive(false);
            timeButton.onClick.AddListener(_presenter.Play);
            speedUpButton.onClick.AddListener(_presenter.SpeedUp);
            reinforcementButton.onClick.AddListener(_presenter.ToggleReinforcement);
            videoModeButton.onClick.AddListener(_presenter.StartCinematic);
            clearFleetButton.onClick.AddListener(_presenter.ClearFleetSelection);
            // Hidden until a super weapon presenter binds; battles without super weapons have none.
            superWeaponsView.SetAvailable(false);
            unitActionsView.SetAvailable(false);
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
            SetReinforcementReady(false);
            _isInitialized = false;
        }

        public void SetReinforcementReady(bool ready)
        {
            reinforcementReadyIndicator.SetActive(ready);
        }

        public void SetHudStatus(string faction, int level, int selectionCount, bool battleEnded)
        {
            var status = (battleEnded, IsVisible);
            if (_lastHudStatus == status) return;
            _lastHudStatus = status;
            timeButton.interactable = !battleEnded && _hasTimeControls;
            speedUpButton.interactable = !battleEnded && _hasTimeControls;
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

        public void SetBattleDurationVisible(bool visible) => battleDurationPanel.SetActive(visible);

        public void SetBattleDuration(int seconds)
        {
            if (_displayedDurationSeconds == seconds) return;
            _displayedDurationSeconds = seconds;
            battleDurationText.text = BattleDuration.Format(seconds);
        }

        /// <summary>Online battles share one time scale, so a client cannot pause or speed it up.</summary>
        public void DisableTimeControls()
        {
            _hasTimeControls = false;
            timeButton.interactable = false;
            speedUpButton.interactable = false;
        }

        public void SetMode(string label, bool spectator)
        {
            _isSpectator = spectator;
            reinforcementButton.gameObject.SetActive(!spectator);
            UpdateShipActionsVisibility();
        }

        public void SetPopulationCapacity(int current, int max)
        {
            populationText.text = $"{current} / {max}";
            populationDisplay.SetActive(true);
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
            _isContentVisible = isVisible;
            panelImage.gameObject.SetActive(isVisible);
            UpdateShipActionsVisibility();
        }

        private void UpdateShipActionsVisibility() =>
            unitActionsView.SetAvailable(_isContentVisible && !_isFactionLayout && !_isSpectator);

        public void SetContentLayout(bool isFactionSelection, bool isShipGroupSelection)
        {
            if (_hasContentLayout && _isFactionLayout == isFactionSelection &&
                _isShipGroupLayout == isShipGroupSelection) return;
            _hasContentLayout = true;
            _isFactionLayout = isFactionSelection;
            _isShipGroupLayout = isShipGroupSelection;
            UiContentLayout.SetPanelLayout(panelImage.rectTransform, isFactionSelection);
            selectionSlots.SetActive(!isFactionSelection);
            clearFleetButton.gameObject.SetActive(isShipGroupSelection);
            UpdateShipActionsVisibility();
        }

    }
}
