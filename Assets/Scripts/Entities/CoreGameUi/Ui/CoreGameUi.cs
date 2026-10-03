using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Commands.Game;
using System;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Presenters.Game;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using MPUIKIT;
using Utilities.ScriptUtils.EditorSerialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Views.Game
{
    public class CoreGameUi : BaseUi, ICoreGameUi, ITooltipHoverView
    {
        private ISkirmishSessionModelObserver _model;
        private ICoreGamePresenter _presenter;

        [SerializeField] private Button timeButton;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Button reinforcementButton;
        [SerializeField] private Button videoModeButton;
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        [SerializeField] private MPImage panelImage;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> speedUpSprites;
        [SerializeField] private Transform miniMapRouteParent;
        [SerializeField] private Transform contentRouteParent;
        [SerializeField] private GridLayoutGroup contentGrid;
        [SerializeField] private ContentSizeFitter contentSizeFitter;
        [SerializeField] private ScrollRect contentScroll;
        [SerializeField] private Transform buildPipelineRouteParent;
        [SerializeField] private EndGameUi endGameUi;
        [SerializeField] private UnitActionsView unitActionsView;
        [SerializeField] private SuperWeaponsView superWeaponsView;
        [SerializeField] private Transform economyRouteParent;
        [SerializeField] private TMP_Text factionText;
        [SerializeField] private TMP_Text contentTitle;
        [SerializeField] private GameObject productionLabel;
        [SerializeField] private GameObject selectionSlots;
        [SerializeField] private Button clearFleetButton;
        [SerializeField] private CanvasGroup battleControls;

        [SerializeField] private Vector2 factionCellSize = new Vector2(150f, 150f);
        [SerializeField] private Vector2 shipCellSize = new Vector2(80f, 198f);

        private bool _isInitialized;
        private bool _hasContentLayout;
        private bool _isFactionLayout;
        private bool _isShipGroupLayout;

        public TooltipHoverView TooltipHover => tooltipHover;

        public IUnitActionsView UnitActionsView => unitActionsView;
        public ISuperWeaponsView SuperWeaponsView => superWeaponsView;

        public void Initialize()
        {
            timeButton.onClick.AddListener(_presenter.Play);
            speedUpButton.onClick.AddListener(_presenter.SpeedUp);
            reinforcementButton.onClick.AddListener(_presenter.ToggleReinforcement);
            videoModeButton.onClick.AddListener(_presenter.StartCinematic);
            clearFleetButton.onClick.AddListener(_presenter.ClearFleetSelection);
            _model.OnGameTimeModeChanged += UpdateSprites;
            UpdateSprites(_model.EffectiveTimeMode);
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
            _model.OnGameTimeModeChanged -= UpdateSprites;
            _isInitialized = false;
        }

        public void SetHudStatus(string faction, int level, int selectionCount, bool battleEnded)
        {
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

        public void SetModel(ISkirmishSessionModelObserver model)
        {
            _model = model;
        }

        public void SetPresenter(ICoreGamePresenter presenter)
        {
            _presenter = presenter;
        }

        public IEndGameView PrepareEndGameView(Transform parent)
        {
            endGameUi.SetParent(parent);
            return endGameUi;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void SetContentVisible(bool isVisible)
        {
            panelImage.gameObject.SetActive(isVisible);
        }

        private void UpdateSprites(GameTimeMode gameTimeMode)
        {
            timeImage.sprite = timeSprites.Dictionary[gameTimeMode];
            speedUpImage.sprite = speedUpSprites.Dictionary[gameTimeMode];
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
            contentScroll.viewport.offsetMin = new Vector2(14f, 12f);
            contentScroll.viewport.offsetMax = new Vector2(-14f, hasHeader ? -42f : -12f);
            RectTransform content = (RectTransform)contentRouteParent;
            contentGrid.enabled = !isShipGroupSelection;
            contentGrid.cellSize = isFactionSelection ? factionCellSize : shipCellSize;
            contentGrid.spacing = new Vector2(8f, 6f);
            contentGrid.padding = new RectOffset();
            contentGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            contentGrid.startAxis = GridLayoutGroup.Axis.Vertical;
            contentGrid.childAlignment = TextAnchor.UpperLeft;
            contentGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            contentGrid.constraintCount = isFactionSelection ? 2 : 1;
            contentSizeFitter.horizontalFit = isShipGroupSelection
                ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(isShipGroupSelection ? 1f : 0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            contentScroll.horizontal = true;
            contentScroll.vertical = false;
            contentScroll.horizontalNormalizedPosition = 0f;
        }

        public Transform GetRouteParent(SkirmishUiRoutePosition position)
        {
            switch (position)
            {
                case SkirmishUiRoutePosition.MiniMap:
                    return miniMapRouteParent;
                case SkirmishUiRoutePosition.Content:
                    return contentRouteParent;
                case SkirmishUiRoutePosition.BuildPipeline:
                    return buildPipelineRouteParent;
                case SkirmishUiRoutePosition.Economy:
                    return economyRouteParent;
                case SkirmishUiRoutePosition.Reinforcement:
                case SkirmishUiRoutePosition.SuperWeapon:
                    return transform;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

    }
}
