using System;
using EmpireAtWar.Models.Players;
using System.Threading;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Input;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishOrchestratorTests
    {
        private SkirmishOrchestrator _orchestrator;
        private GameCommandStub _gameCommand;
        private InputLockStub _inputLock;
        private StateRecorder _state;
        private SpeedRecorder _speed;

        [SetUp]
        public void SetUp()
        {
            _gameCommand = new GameCommandStub();
            _inputLock = new InputLockStub();
            DiContainer container = new DiContainer();
            container.Bind<IUserStateNotifier>().FromInstance(new UserStateNotifierStub());
            container.Bind<INotifier<BattleResult>>().FromInstance(new NotifierStub<BattleResult>());
            _orchestrator = new SkirmishOrchestrator(
                gameCommand: _gameCommand,
                startupSequence: new CompletedStartupStub(),
                inputLock: _inputLock,
                battleVictoryNotifier: new LazyInject<INotifier<BattleResult>>(container,
                    new InjectContext(container, typeof(INotifier<BattleResult>))),
                userStateNotifier: new LazyInject<IUserStateNotifier>(container,
                    new InjectContext(container, typeof(IUserStateNotifier))));
            _state = new StateRecorder();
            _speed = new SpeedRecorder();
            _orchestrator.AddObserver(_state);
            _orchestrator.AddObserver(_speed);
            _orchestrator.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
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

            _orchestrator.AddObserver(lateState);
            _orchestrator.AddObserver(lateSpeed);

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
            public void AddObserver(IObserver<T> observer) { }

            public void RemoveObserver(IObserver<T> observer) { }
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
