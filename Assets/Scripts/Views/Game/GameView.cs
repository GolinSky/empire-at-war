using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Game
{
    //todo: refactor
    public class GameView : View<ISkirmishGameModelObserver, ISkirmishGameCommand>
    {
        [SerializeField] private Button timeButton;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> speedUpSprites;
        
        protected override void OnInitialize()
        {
            timeButton.onClick.AddListener(HandleTimeChange);
            speedUpButton.onClick.AddListener(HandleSpeedUp);
        }

        protected override void OnDispose()
        {
            timeButton.onClick.RemoveListener(HandleTimeChange);
            speedUpButton.onClick.RemoveListener(HandleSpeedUp);
        }
        
        private void HandleSpeedUp()
        {
            switch (Model.GameTimeMode )
            {
                case GameTimeMode.Common:
                case GameTimeMode.Pause:
                    Command.ChangeTime(GameTimeMode.SpeedUp);
                    break;
                case GameTimeMode.SpeedUp:
                    Command.ChangeTime(GameTimeMode.Common);
                    break;
            }
            speedUpImage.sprite = speedUpSprites.Dictionary[Model.GameTimeMode];
        }

        private void HandleTimeChange()
        {
            switch (Model.GameTimeMode )
            {
                case GameTimeMode.Common:
                    Command.ChangeTime(GameTimeMode.Pause);
                    break;
                case GameTimeMode.Pause:
                    Command.ChangeTime(GameTimeMode.Common);
                    break;
                
                case GameTimeMode.SpeedUp:
                    Command.ChangeTime(GameTimeMode.Pause);
                    speedUpImage.sprite = speedUpSprites.Dictionary[GameTimeMode.Common];
                    break;
            }
            timeImage.sprite = timeSprites.Dictionary[Model.GameTimeMode];
        }
    }
}