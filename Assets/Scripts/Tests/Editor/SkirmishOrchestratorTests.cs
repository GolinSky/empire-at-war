using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System.Threading;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Input;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishOrchestratorTests
    {
        private SkirmishOrchestrator _orchestrator;
        private SkirmishStateModel _stateModel;
        private GameCommandStub _gameCommand;
        private InputLockStub _inputLock;
        private StateRecorder _state;
        private SpeedRecorder _speed;
        private SkirmishFlowSubscriptions _subscriptions;
        private UserStateNotifierStub _userStateNotifier;
        private NotifierStub<BattleResult> _battleVictoryNotifier;

        [SetUp]
        public void SetUp()
        {
            _gameCommand = new GameCommandStub();
            _inputLock = new InputLockStub();
            _stateModel = new SkirmishStateModel();
            _orchestrator = new SkirmishOrchestrator(
                gameCommand: _gameCommand,
                startupSequence: new CompletedStartupStub(),
                inputLock: _inputLock,
                stateModel: _stateModel);
            _userStateNotifier = new UserStateNotifierStub();
            _battleVictoryNotifier = new NotifierStub<BattleResult>();
            _subscriptions = new SkirmishFlowSubscriptions(
                _orchestrator, _battleVictoryNotifier, _userStateNotifier);
            _state = new StateRecorder();
            _speed = new SpeedRecorder();
            _stateModel.AddObserver(_state);
            _stateModel.AddObserver(_speed);
            _subscriptions.Initialize();
            _orchestrator.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _subscriptions.LateDispose();
            _orchestrator.LateDispose();
            Time.timeScale = 1f;
        }

        [Test]
        public void Startup_RunsBattleAndReleasesInputLock()
        {
            Assert.That(_state.Value, Is.EqualTo(BattleState.Running));
            Assert.That(_inputLock.ActiveHandles, Is.Zero);
            Assert.That(_inputLock.AcquireCount, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void AddObserver_ReplaysCurrentValues()
        {
            StateRecorder lateState = new StateRecorder();
            SpeedRecorder lateSpeed = new SpeedRecorder();

            _stateModel.AddObserver(lateState);
            _stateModel.AddObserver(lateSpeed);

            Assert.That(lateState.Value, Is.EqualTo(BattleState.Running));
            Assert.That(lateSpeed.Value, Is.EqualTo(GameSpeed.Normal));
        }

        [Test]
        public void TogglePause_SwitchesBetweenRunningAndPaused()
        {
            _orchestrator.TogglePause();
            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));
            Assert.That(Time.timeScale, Is.Zero);

            _orchestrator.TogglePause();
            Assert.That(_state.Value, Is.EqualTo(BattleState.Running));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ToggleSpeedUp_SwitchesSpeedAndKeepsPause()
        {
            _orchestrator.ToggleSpeedUp();
            Assert.That(_speed.Value, Is.EqualTo(GameSpeed.Fast));
            Assert.That(Time.timeScale, Is.EqualTo(4f));

            _orchestrator.TogglePause();
            _orchestrator.ToggleSpeedUp();
            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));
            Assert.That(_speed.Value, Is.EqualTo(GameSpeed.Normal));
            Assert.That(Time.timeScale, Is.Zero);

            _orchestrator.ToggleSpeedUp();
            _orchestrator.TogglePause();
            Assert.That(Time.timeScale, Is.EqualTo(4f));
        }

        [Test]
        public void TimeControls_DoNothingAfterBattleEnds()
        {
            _orchestrator.UpdateState(new BattleResult(
                outcome: default, victoryCondition: default, planet: default, playerFaction: default, enemyFactions: default, playerShipCount: 0, enemyShipCount: 0, isPlayerBaseAlive: false, isEnemyBaseAlive: false));

            _orchestrator.TogglePause();
            _orchestrator.ToggleSpeedUp();

            Assert.That(_state.Value, Is.EqualTo(BattleState.Ended));
            Assert.That(_speed.Value, Is.EqualTo(GameSpeed.Normal));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void MenuPause_BlocksTimeControls()
        {
            _orchestrator.ToggleSpeedUp();
            _orchestrator.UpdateState(UserNotifierState.InMenu);
            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));
            Assert.That(Time.timeScale, Is.Zero);

            _orchestrator.TogglePause();
            _orchestrator.ToggleSpeedUp();

            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));
            Assert.That(_speed.Value, Is.EqualTo(GameSpeed.Fast));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [TestCase(false, 4f)]
        [TestCase(true, 0f)]
        public void MenuClose_RestoresStateBeforeMenu(bool isPaused, float scale)
        {
            _orchestrator.ToggleSpeedUp();
            if (isPaused)
            {
                _orchestrator.TogglePause();
            }

            _orchestrator.UpdateState(UserNotifierState.InMenu);
            _orchestrator.UpdateState(UserNotifierState.InGame);

            Assert.That(_state.Value, Is.EqualTo(isPaused ? BattleState.Paused : BattleState.Running));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
        }

        [Test]
        public void ExitSkirmish_RestoresNormalTimeAndExitsGame()
        {
            _orchestrator.TogglePause();

            _orchestrator.ExitSkirmish();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_gameCommand.ExitCount, Is.EqualTo(1));
        }

        [Test]
        public void UserStateSubscription_PausesBattleAndStopsAfterDisposal()
        {
            _userStateNotifier.Publish(UserNotifierState.InMenu);
            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));

            _subscriptions.LateDispose();
            _userStateNotifier.Publish(UserNotifierState.InGame);

            Assert.That(_state.Value, Is.EqualTo(BattleState.Paused));
        }

        [Test]
        public void BattleResultSubscription_EndsBattleAndStopsAfterDisposal()
        {
            BattleResult result = new BattleResult(
                outcome: default, victoryCondition: default, planet: default, playerFaction: default,
                enemyFactions: default, playerShipCount: 0, enemyShipCount: 0,
                isPlayerBaseAlive: false, isEnemyBaseAlive: false);
            _subscriptions.LateDispose();
            _battleVictoryNotifier.Publish(result);
            Assert.That(_state.Value, Is.EqualTo(BattleState.Running));

            _subscriptions.Initialize();
            _battleVictoryNotifier.Publish(result);

            Assert.That(_state.Value, Is.EqualTo(BattleState.Ended));
        }

        [Test]
        public void StateModel_ReplaysInitialStateBeforeStartup()
        {
            SkirmishStateModel model = new SkirmishStateModel();
            StateRecorder state = new StateRecorder();
            SpeedRecorder speed = new SpeedRecorder();

            model.AddObserver(state);
            model.AddObserver(speed);

            Assert.That(state.Value, Is.EqualTo(BattleState.NotInitialized));
            Assert.That(speed.Value, Is.EqualTo(GameSpeed.Normal));
        }

        private sealed class CompletedStartupStub : IBattleStartupSequence
        {
            public Awaitable RunAsync(CancellationToken cancellationToken)
            {
                AwaitableCompletionSource source = new AwaitableCompletionSource();
                source.SetResult();
                return source.Awaitable;
            }
        }

        private sealed class StateRecorder : IObserver<BattleState>
        {
            public BattleState Value { get; private set; }

            public void UpdateState(BattleState value) => Value = value;
        }

        private sealed class SpeedRecorder : IObserver<GameSpeed>
        {
            public GameSpeed Value { get; private set; }

            public void UpdateState(GameSpeed value) => Value = value;
        }

        private sealed class InputLockStub : IInputLock
        {
            public event Action<bool> LockChanged { add { } remove { } }

            public int AcquireCount { get; private set; }
            public int ActiveHandles { get; private set; }
            public bool IsLocked => ActiveHandles > 0;

            public IDisposable Acquire()
            {
                AcquireCount++;
                ActiveHandles++;
                return new Handle(this);
            }

            public IDisposable AcquireBattle() => throw new NotSupportedException();

            private sealed class Handle : IDisposable
            {
                private readonly InputLockStub _owner;

                public Handle(InputLockStub owner) => _owner = owner;

                public void Dispose() => _owner.ActiveHandles--;
            }
        }

        private class NotifierStub<T> : INotifier<T>
        {
            private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

            public void AddObserver(IObserver<T> observer) => _observers.Add(observer);

            public void RemoveObserver(IObserver<T> observer) => _observers.Remove(observer);

            public void Publish(T value)
            {
                foreach (IObserver<T> observer in _observers)
                {
                    observer.UpdateState(value);
                }
            }
        }

        private sealed class UserStateNotifierStub : NotifierStub<UserNotifierState>, IUserStateNotifier
        {
        }

        private sealed class GameCommandStub : IGameCommand
        {
            public int ExitCount { get; private set; }

            public void ExitGame() => ExitCount++;

            public void StartGame(
                System.Collections.Generic.IReadOnlyList<PlayerSlot> players,
                PlanetType planetType,
                MapSize mapSize,
                BattleVictoryCondition victoryCondition,
                float startingMoney) { }
        }
    }
}
