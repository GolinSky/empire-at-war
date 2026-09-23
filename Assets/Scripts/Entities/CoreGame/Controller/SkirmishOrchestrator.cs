using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public class SkirmishOrchestrator : ISkirmishFlow, IInitializable, ILateDisposable,
        IObserver<UserNotifierState>, IObserver<BattleResult>
    {
        private const float SPEED_UP_TIME_SCALE = 4f;
        private const float DEFAULT_TIME_SCALE = 1f;
        private const float PAUSE_TIME_SCALE = 0f;

        private readonly SkirmishSessionModel _sessionModel;
        private readonly LazyInject<IUserStateNotifier> _userStateNotifier;
        private readonly IGameCommand _gameCommand;
        private readonly ICameraService _cameraService;
        private readonly IMapModelObserver _mapModel;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly FactionType _playerFactionType;
        private GameTimeMode _gameTimeMode;

        public SkirmishOrchestrator(
            SkirmishSessionModel sessionModel,
            LazyInject<IUserStateNotifier> userStateNotifier,
            IGameCommand gameCommand,
            ICameraService cameraService,
            IMapModelObserver mapModel,
            INotifier<BattleResult> battleVictoryNotifier,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType)
        {
            _sessionModel = sessionModel;
            _userStateNotifier = userStateNotifier;
            _gameCommand = gameCommand;
            _cameraService = cameraService;
            _mapModel = mapModel;
            _battleVictoryNotifier = battleVictoryNotifier;
            _playerFactionType = playerFactionType;
            _gameTimeMode = GameTimeMode.Common;
        }

        public void Initialize()
        {
            ChangeTime(_gameTimeMode);
            _userStateNotifier.Value.AddObserver(this);
            _battleVictoryNotifier.AddObserver(this);
            _cameraService.MoveTo(_mapModel.GetStationPosition(_playerFactionType));
        }

        public void LateDispose()
        {
            _userStateNotifier.Value.RemoveObserver(this);
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void TogglePause()
        {
            if (_sessionModel.IsBattleEnded)
            {
                return;
            }

            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_gameTimeMode);
        }

        public void ToggleSpeedUp()
        {
            if (_sessionModel.IsBattleEnded)
            {
                return;
            }

            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.SpeedUp;
                    break;
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_gameTimeMode);
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ExitSkirmish();
                return;
            }

            if (_sessionModel.IsBattleEnded)
            {
                return;
            }

            ChangeTime(
                notifierState == UserNotifierState.InMenu
                    ? GameTimeMode.Pause
                    : GameTimeMode.Common);
        }

        public void UpdateState(BattleResult result)
        {
            _sessionModel.MarkBattleEnded();
            ChangeTime(GameTimeMode.Pause);
        }

        public void ExitSkirmish()
        {
            ChangeTime(GameTimeMode.Common);
            _gameCommand.ExitGame();
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

            _sessionModel.SetGameTimeMode(mode);
        }
    }
}
