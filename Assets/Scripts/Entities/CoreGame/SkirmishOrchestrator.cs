using EmpireAtWar.Entities.CoreGameUi;
using System;
using System.Threading;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Input;
using UnityEngine;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Controllers.Game
{
    /// <summary>
    /// Runs the battle startup and owns <see cref="BattleState"/>, <see cref="GameSpeed"/> and the time scale.
    /// </summary>
    public class SkirmishOrchestrator : ISkirmishFlow, IInitializable, ILateDisposable,
        IObserver<UserNotifierState>, IObserver<BattleResult>
    {
        private const float FAST_TIME_SCALE = 4f;
        private const float NORMAL_TIME_SCALE = 1f;
        private const float PAUSE_TIME_SCALE = 0f;

        private readonly IGameCommand _gameCommand;
        private readonly IBattleStartupSequence _startupSequence;
        private readonly IInputLock _inputLock;

        private readonly SkirmishStateModel _stateModel;
        private readonly CancellationTokenSource _startupCancellation = new CancellationTokenSource();

        private IDisposable _loadingInputLock;
        private bool _isMenuOpen;
        private bool _isPausedBeforeMenu;

        public SkirmishOrchestrator(
            IGameCommand gameCommand,
            IBattleStartupSequence startupSequence,
            IInputLock inputLock,
            SkirmishStateModel stateModel)
        {
            _gameCommand = gameCommand;
            _startupSequence = startupSequence;
            _inputLock = inputLock;
            _stateModel = stateModel;
        }

        public void Initialize()
        {
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
        }

        public void TogglePause()
        {
            if (_isMenuOpen)
            {
                return;
            }

            if (_stateModel.State == BattleState.Running)
            {
                SetState(BattleState.Paused);
            }
            else if (_stateModel.State == BattleState.Paused)
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

            _stateModel.SetSpeed(_stateModel.Speed == GameSpeed.Normal ? GameSpeed.Fast : GameSpeed.Normal);
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
                _isPausedBeforeMenu = _stateModel.State == BattleState.Paused;
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
            return _stateModel.State == BattleState.Running || _stateModel.State == BattleState.Paused;
        }

        private void SetState(BattleState state)
        {
            // The battle stays ended even if the startup finishes afterwards.
            if (_stateModel.State == BattleState.Ended)
            {
                return;
            }

            _stateModel.SetState(state);
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            switch (_stateModel.State)
            {
                case BattleState.Running:
                    Time.timeScale = _stateModel.Speed == GameSpeed.Fast ? FAST_TIME_SCALE : NORMAL_TIME_SCALE;
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
