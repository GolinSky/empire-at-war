using System.Globalization;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Models.SkirmishGame;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Views.ViewImpl;
using TMPro;
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
        [SerializeField] private TextMeshProUGUI moneyText;
        
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> timeSprites;
        [SerializeField] private DictionaryWrapper<GameTimeMode, Sprite> speedUpSprites;
        
        
        protected override void OnInitialize()
        {
            timeButton.onClick.AddListener(Command.Play);
            speedUpButton.onClick.AddListener(Command.SpeedUp);
            Model.OnGameTimeModeChange += UpdateSprites;
            Model.OnMoneyChanged += UpdateMoneyText;
        }

        protected override void OnDispose()
        {
            timeButton.onClick.RemoveListener(Command.Play);
            speedUpButton.onClick.RemoveListener(Command.SpeedUp);
            Model.OnGameTimeModeChange -= UpdateSprites;
        }
        
        private void UpdateMoneyText(float money)
        {
            moneyText.text = money.ToString(CultureInfo.InvariantCulture);

        }
        
        private void UpdateSprites(GameTimeMode gameTimeMode)
        {
            timeImage.sprite = timeSprites.Dictionary[gameTimeMode];
            speedUpImage.sprite = speedUpSprites.Dictionary[gameTimeMode];
        }

    }
}