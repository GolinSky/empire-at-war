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
        [SerializeField] private Transform buildPipelineRouteParent;
        [SerializeField] private EndGameUi endGameUi;

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
            panelImage.enabled = isVisible;
        }
        
        private void UpdateSprites(GameTimeMode gameTimeMode)
        {
            timeImage.sprite = timeSprites.Dictionary[gameTimeMode];
            speedUpImage.sprite = speedUpSprites.Dictionary[gameTimeMode];
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
