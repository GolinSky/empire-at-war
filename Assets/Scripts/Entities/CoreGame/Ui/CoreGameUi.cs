using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
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


        public void Initialize()
        {
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
    }
}