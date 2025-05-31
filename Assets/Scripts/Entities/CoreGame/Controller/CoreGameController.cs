using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public class CoreGameController : Controller<CoreGameModel>, ICoreGameCommand, IObserver<UserNotifierState>, IInitializable, ILateDisposable
    {
        private const float SPEED_UP_TIME_SCALE = 4f;
        private const float DEFAULT_TIME_SCALE = 1f;
        private const float PAUSE_TIME_SCALE = 0f;
        private readonly LazyInject<IUserStateNotifier> _userStateNotifier;
        private readonly IGameCommand _gameCommand;
        private readonly IUiService _uiService;
        private readonly FactionsModel _factionsModel;
        private GameTimeMode _gameTimeMode;
        
        public CoreGameController(
            CoreGameModel model,
            LazyInject<IUserStateNotifier> userStateNotifier,
            IGameCommand gameCommand,
            IUiService uiService) : base(model)
        {
            _userStateNotifier = userStateNotifier;
            _gameCommand = gameCommand;
            _uiService = uiService;
            _gameTimeMode = GameTimeMode.Common;
            ChangeTime(_gameTimeMode);
        }
        
        public void Initialize()
        {
            _userStateNotifier.Value.AddObserver(this);
            _uiService.CreateUi(UiType.CoreGame);
        }

        public void LateDispose()
        {
            _userStateNotifier.Value.RemoveObserver(this);
        }

        public void Play()
        {
            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                    _gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_gameTimeMode);
        }

        public void SpeedUp()
        {
            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                    _gameTimeMode = GameTimeMode.SpeedUp;
                    break;
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.SpeedUp;
                    break;
            }
            ChangeTime(_gameTimeMode);
        }
        
        private void ChangeTime(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Common:
                    Time.timeScale = DEFAULT_TIME_SCALE;
                    break;
                case GameTimeMode.SpeedUp:
                    Time.timeScale = SPEED_UP_TIME_SCALE;
                    break;
                case GameTimeMode.Pause:
                    Time.timeScale = PAUSE_TIME_SCALE;
                    break;
            }
            Model.GameTimeMode = mode;
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ChangeTime(GameTimeMode.Common);
                _gameCommand.ExitGame();
                return;
            }
            ChangeTime(notifierState == UserNotifierState.InMenu ? GameTimeMode.Pause : GameTimeMode.Common);
        }
    }
}