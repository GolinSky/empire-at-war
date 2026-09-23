using EmpireAtWar.Commands.Game;
using System;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using MPUIKIT;
using Utilities.ScriptUtils.EditorSerialization;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Game
{
    public class CoreGameUi : BaseUi<ICoreGameModelObserver, ICoreGameCommand>, IInitializable, ILateDisposable
    {
        [SerializeField] private Button timeButton;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Button reinforcementButton;
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

        private bool _isShipGroupLayout;

        public IEndGameView PrepareEndGameView(Transform parent)
        {
            endGameUi.SetParent(parent);
            return endGameUi;
        }


        public void Initialize()
        {
            timeButton.onClick.AddListener(Command.Play);
            speedUpButton.onClick.AddListener(Command.SpeedUp);
            reinforcementButton.onClick.AddListener(Command.ToggleReinforcement);
            Model.OnGameTimeModeChange += UpdateSprites;
            Model.OnContentVisibilityChanged += HandleContentVisibilityChanged;
            SetContentPanelVisible(Model.IsContentVisible);
        }

        public void LateDispose()
        {
            timeButton.onClick.RemoveListener(Command.Play);
            speedUpButton.onClick.RemoveListener(Command.SpeedUp);
            reinforcementButton.onClick.RemoveListener(Command.ToggleReinforcement);
            Model.OnGameTimeModeChange -= UpdateSprites;
            Model.OnContentVisibilityChanged -= HandleContentVisibilityChanged;
        }

        private void HandleContentVisibilityChanged(bool isVisible)
        {
            SetContentPanelVisible(isVisible);
        }

        private void SetContentPanelVisible(bool isVisible)
        {
            panelImage.gameObject.SetActive(isVisible);
        }
        
        private void UpdateSprites(GameTimeMode gameTimeMode)
        {
            timeImage.sprite = timeSprites.Dictionary[gameTimeMode];
            speedUpImage.sprite = speedUpSprites.Dictionary[gameTimeMode];
        }

        public void SetShipGroupLayout(bool isShipSelection)
        {
            if (_isShipGroupLayout == isShipSelection)
            {
                if (isShipSelection)
                {
                    contentScroll.horizontalNormalizedPosition = 0f;
                }

                return;
            }

            _isShipGroupLayout = isShipSelection;
            RectTransform content = (RectTransform)contentRouteParent;
            if (isShipSelection)
            {
                contentGrid.cellSize = new Vector2(332f, 332f);
                contentGrid.spacing = new Vector2(16f, 0f);
                contentGrid.padding = new RectOffset(18, 18, 18, 18);
                contentGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                contentGrid.constraintCount = 1;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                contentScroll.horizontal = true;
                contentScroll.vertical = false;
                contentScroll.horizontalNormalizedPosition = 0f;
            }
            else
            {
                contentGrid.cellSize = new Vector2(150f, 150f);
                contentGrid.spacing = new Vector2(20f, 20f);
                contentGrid.padding = new RectOffset(36, 36, 18, 18);
                contentGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                contentGrid.constraintCount = 2;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                contentScroll.horizontal = false;
                contentScroll.vertical = true;
                contentScroll.verticalNormalizedPosition = 1f;
            }
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
                case SkirmishUiRoutePosition.Reinforcement:
                    return transform;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

    }
}
