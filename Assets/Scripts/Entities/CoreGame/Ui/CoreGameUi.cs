using EmpireAtWar.Commands.Game;
using System;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
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
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> speedUpSprites;
        [SerializeField] private Transform miniMapRouteParent;
        [SerializeField] private Transform contentRouteParent;
        [SerializeField] private Transform buildPipelineRouteParent;


        public void Initialize()
        {
            ValidateRouteParents();
            timeButton.onClick.AddListener(Command.Play);
            speedUpButton.onClick.AddListener(Command.SpeedUp);
            Model.OnGameTimeModeChange += UpdateSprites;
        }

        public void LateDispose()
        {
            timeButton.onClick.RemoveListener(Command.Play);
            speedUpButton.onClick.RemoveListener(Command.SpeedUp);
            Model.OnGameTimeModeChange -= UpdateSprites;
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        private void ValidateRouteParents()
        {
            if (miniMapRouteParent == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(miniMapRouteParent)} is not assigned.");
            }

            if (contentRouteParent == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(contentRouteParent)} is not assigned.");
            }

            if (buildPipelineRouteParent == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(buildPipelineRouteParent)} is not assigned.");
            }
        }
    }
}
