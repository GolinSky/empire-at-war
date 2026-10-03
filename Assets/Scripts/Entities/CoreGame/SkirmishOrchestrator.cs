using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
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

        private readonly IGameCommand _gameCommand;
        private readonly ICameraService _cameraService;
        private readonly IMapModelObserver _mapModel;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly ILocalPlayer _localPlayer;

        private readonly SkirmishSessionModel _sessionModel;
        private readonly LazyInject<IUserStateNotifier> _userStateNotifier;

        private GameTimeMode _requestedTimeMode;

        private bool _isMenuOpen;

        public SkirmishOrchestrator(
            IGameCommand gameCommand,
            ICameraService cameraService,
            IMapModelObserver mapModel,
            INotifier<BattleResult> battleVictoryNotifier,
            ILocalPlayer localPlayer,
            SkirmishSessionModel sessionModel,
            LazyInject<IUserStateNotifier> userStateNotifier)
        {
            _sessionModel = sessionModel;
            _userStateNotifier = userStateNotifier;
            _gameCommand = gameCommand;
            _cameraService = cameraService;
            _mapModel = mapModel;
            _battleVictoryNotifier = battleVictoryNotifier;
            _localPlayer = localPlayer;
            _requestedTimeMode = GameTimeMode.Common;
        }

        public void Initialize()
        {
            ChangeTime(_requestedTimeMode);
            _userStateNotifier.Value.AddObserver(this);
            _battleVictoryNotifier.AddObserver(this);
            _cameraService.MoveTo(_mapModel.GetStationPosition(_localPlayer.Id));
        }

        public void LateDispose()
        {
            _userStateNotifier.Value.RemoveObserver(this);
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void TogglePause()
        {
            if (_sessionModel.IsBattleEnded || _isMenuOpen)
            {
                return;
            }

            switch (_requestedTimeMode)
            {
                case GameTimeMode.Common:
                case GameTimeMode.SpeedUp:
                    _requestedTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.Pause:
                    _requestedTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_requestedTimeMode);
        }

        public void ToggleSpeedUp()
        {
            if (_sessionModel.IsBattleEnded || _isMenuOpen)
            {
                return;
            }

            switch (_requestedTimeMode)
            {
                case GameTimeMode.Common:
                case GameTimeMode.Pause:
                    _requestedTimeMode = GameTimeMode.SpeedUp;
                    break;
                case GameTimeMode.SpeedUp:
                    _requestedTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_requestedTimeMode);
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

            _isMenuOpen = notifierState == UserNotifierState.InMenu;
            ChangeTime(_isMenuOpen ? GameTimeMode.Pause : _requestedTimeMode);
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

        private void ChangeTime(GameTimeMode effectiveMode)
        {
            switch (effectiveMode)
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

            _sessionModel.SetGameTimeMode(effectiveMode);
        }
    }
}
