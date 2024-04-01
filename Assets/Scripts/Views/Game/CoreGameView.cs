using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Game
{
    public class CoreGameView : View<ICoreGameModelObserver, ICoreGameCommand>
    {
        [SerializeField] private Button timeButton;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Image timeImage;
        [SerializeField] private Image speedUpImage;
        
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> speedUpSprites;
        
        
        protected override void OnInitialize()
        {
            timeButton.onClick.AddListener(Command.Play);
            speedUpButton.onClick.AddListener(Command.SpeedUp);
            Model.OnGameTimeModeChange += UpdateSprites;
        }

        protected override void OnDispose()
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
    }
}