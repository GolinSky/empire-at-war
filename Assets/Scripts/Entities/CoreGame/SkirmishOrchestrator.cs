using System;
using System.Threading;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Utils;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    /// <summary>
    /// Runs the battle startup and owns <see cref="BattleState"/>, <see cref="GameSpeed"/> and the time scale.
    /// </summary>
    public class SkirmishOrchestrator : ISkirmishFlow, IInitializable, ILateDisposable,
        INotifier<BattleState>, INotifier<GameSpeed>,
        IObserver<UserNotifierState>, IObserver<BattleResult>
    {
        private const float FAST_TIME_SCALE = 4f;
        private const float NORMAL_TIME_SCALE = 1f;
        private const float PAUSE_TIME_SCALE = 0f;

        private readonly IGameCommand _gameCommand;
        private readonly IBattleStartupSequence _startupSequence;
        private readonly IInputLock _inputLock;
        private readonly LazyInject<INotifier<BattleResult>> _battleVictoryNotifier;
        private readonly LazyInject<IUserStateNotifier> _userStateNotifier;

        private readonly ReplayNotifier<BattleState> _state = new ReplayNotifier<BattleState>(BattleState.NotInitialized);
        private readonly ReplayNotifier<GameSpeed> _speed = new ReplayNotifier<GameSpeed>(GameSpeed.Normal);
        private readonly CancellationTokenSource _startupCancellation = new CancellationTokenSource();

        private IDisposable _loadingInputLock;
        private bool _isMenuOpen;
        private bool _isPausedBeforeMenu;

        public SkirmishOrchestrator(
            IGameCommand gameCommand,
            IBattleStartupSequence startupSequence,
            IInputLock inputLock,
            LazyInject<INotifier<BattleResult>> battleVictoryNotifier,
            LazyInject<IUserStateNotifier> userStateNotifier)
        {
            _gameCommand = gameCommand;
            _startupSequence = startupSequence;
            _inputLock = inputLock;
            _battleVictoryNotifier = battleVictoryNotifier;
            _userStateNotifier = userStateNotifier;
        }

        public void Initialize()
        {
            _userStateNotifier.Value.AddObserver(this);
            _battleVictoryNotifier.Value.AddObserver(this);
            _ = RunStartupAsync(_startupCancellation.Token);
        }

        public void LateDispose()
        {
            _startupCancellation.Cancel();
            _startupCancellation.Dispose();
            if (_loadingInputLock != null)
            {
                _loadingInputLock.Dispose();
            }

            _userStateNotifier.Value.RemoveObserver(this);
            _battleVictoryNotifier.Value.RemoveObserver(this);
        }

        public void AddObserver(IObserver<BattleState> observer) => _state.AddObserver(observer);

        public void RemoveObserver(IObserver<BattleState> observer) => _state.RemoveObserver(observer);

        public void AddObserver(IObserver<GameSpeed> observer) => _speed.AddObserver(observer);

        public void RemoveObserver(IObserver<GameSpeed> observer) => _speed.RemoveObserver(observer);

        public void TogglePause()
        {
            if (_isMenuOpen)
            {
                return;
            }

            if (_state.Value == BattleState.Running)
            {
                SetState(BattleState.Paused);
            }
            else if (_state.Value == BattleState.Paused)
            {
                SetState(BattleState.Running);
            }
        }

        public void ToggleSpeedUp()
        {
            if (_isMenuOpen || !IsPlaying())
            {
                return;
            }

            _speed.Set(_speed.Value == GameSpeed.Normal ? GameSpeed.Fast : GameSpeed.Normal);
            ApplyTimeScale();
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ExitSkirmish();
                return;
            }

            bool isMenuOpen = notifierState == UserNotifierState.InMenu;
            if (isMenuOpen == _isMenuOpen || !IsPlaying())
            {
                return;
            }

            // The menu pauses the battle; closing it restores the state the player left.
            _isMenuOpen = isMenuOpen;
            if (isMenuOpen)
            {
                _isPausedBeforeMenu = _state.Value == BattleState.Paused;
            }

            SetState(isMenuOpen || _isPausedBeforeMenu ? BattleState.Paused : BattleState.Running);
        }

        public void UpdateState(BattleResult result)
        {
            SetState(BattleState.Ended);
        }

        public void ExitSkirmish()
        {
            Time.timeScale = NORMAL_TIME_SCALE;
            _startupCancellation.Cancel();
            _gameCommand.ExitGame();
        }

        private async Awaitable RunStartupAsync(CancellationToken cancellationToken)
        {
            try
            {
                SetState(BattleState.Loading);
                _loadingInputLock = _inputLock.Acquire();
                await _startupSequence.RunAsync(cancellationToken);
                _loadingInputLock.Dispose();
                _loadingInputLock = null;
                SetState(BattleState.Running);
            }
            catch (OperationCanceledException)
            {
                // The scene is closing; no step may run after teardown.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private bool IsPlaying()
        {
            return _state.Value == BattleState.Running || _state.Value == BattleState.Paused;
        }

        private void SetState(BattleState state)
        {
            // The battle stays ended even if the startup finishes afterwards.
            if (_state.Value == BattleState.Ended)
            {
                return;
            }

            _state.Set(state);
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            switch (_state.Value)
            {
                case BattleState.Running:
                    Time.timeScale = _speed.Value == GameSpeed.Fast ? FAST_TIME_SCALE : NORMAL_TIME_SCALE;
                    break;
                case BattleState.Paused:
                case BattleState.Ended:
                    Time.timeScale = PAUSE_TIME_SCALE;
                    break;
                default:
                    Time.timeScale = NORMAL_TIME_SCALE;
                    break;
            }
        }
    }
}
