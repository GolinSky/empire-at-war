using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.Camera;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishOrchestratorTests
    {
        private SkirmishSessionModel _model;
        private SkirmishOrchestrator _orchestrator;
        private GameCommandStub _gameCommand;

        [SetUp]
        public void SetUp()
        {
            _model = new SkirmishSessionModel();
            _gameCommand = new GameCommandStub();
            DiContainer container = new DiContainer();
            container.Bind<IUserStateNotifier>().FromInstance(new UserStateNotifierStub());
            _orchestrator = new SkirmishOrchestrator(
                _model,
                new LazyInject<IUserStateNotifier>(container,
                    new InjectContext(container, typeof(IUserStateNotifier))),
                _gameCommand,
                new CameraServiceStub(),
                new MapModelStub(),
                new NotifierStub<BattleResult>(),
                FactionType.Republic);
            _orchestrator.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _orchestrator.LateDispose();
            Time.timeScale = 1f;
        }

        [TestCase(GameTimeMode.Common, false, GameTimeMode.Pause, 0f)]
        [TestCase(GameTimeMode.SpeedUp, false, GameTimeMode.Pause, 0f)]
        [TestCase(GameTimeMode.Pause, false, GameTimeMode.Common, 1f)]
        [TestCase(GameTimeMode.Common, true, GameTimeMode.SpeedUp, 4f)]
        [TestCase(GameTimeMode.SpeedUp, true, GameTimeMode.Common, 1f)]
        [TestCase(GameTimeMode.Pause, true, GameTimeMode.SpeedUp, 4f)]
        public void TimeControls_FollowTransitionTable(
            GameTimeMode initialMode,
            bool speedUp,
            GameTimeMode expectedMode,
            float expectedScale)
        {
            SetStoredMode(initialMode);

            if (speedUp)
            {
                _orchestrator.ToggleSpeedUp();
            }
            else
            {
                _orchestrator.TogglePause();
            }

            Assert.That(_model.GameTimeMode, Is.EqualTo(expectedMode));
            Assert.That(Time.timeScale, Is.EqualTo(expectedScale));
        }

        [Test]
        public void TimeControls_DoNothingAfterBattleEnds()
        {
            _orchestrator.UpdateState(new BattleResult(
                default, default, default, default, default, 0, 0, false, false));

            _orchestrator.TogglePause();
            _orchestrator.ToggleSpeedUp();

            Assert.That(_model.IsBattleEnded, Is.True);
            Assert.That(_model.GameTimeMode, Is.EqualTo(GameTimeMode.Pause));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void MenuPause_DoesNotChangeStoredMode()
        {
            _orchestrator.ToggleSpeedUp();
            _orchestrator.UpdateState(UserNotifierState.InMenu);
            Assert.That(_model.GameTimeMode, Is.EqualTo(GameTimeMode.Pause));
            Assert.That(Time.timeScale, Is.Zero);

            _orchestrator.TogglePause();

            Assert.That(_model.GameTimeMode, Is.EqualTo(GameTimeMode.Pause));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void ExitSkirmish_RestoresCommonTimeAndExitsGame()
        {
            _orchestrator.TogglePause();

            _orchestrator.ExitSkirmish();

            Assert.That(_model.GameTimeMode, Is.EqualTo(GameTimeMode.Common));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_gameCommand.ExitCount, Is.EqualTo(1));
        }

        private void SetStoredMode(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.SpeedUp:
                    _orchestrator.ToggleSpeedUp();
                    break;
                case GameTimeMode.Pause:
                    _orchestrator.TogglePause();
                    break;
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
                FactionType playerFactionType,
                FactionType enemyFactionType,
                PlanetType planetType,
                BattleVictoryCondition victoryCondition,
                EnemyAiDifficulty enemyDifficulty,
                float startingMoney) { }
        }

        private sealed class MapModelStub : IMapModelObserver
        {
            public Vector2Range SizeRange => null;
            public Vector3 GetStationPosition(FactionType factionType) => Vector3.zero;
        }

        private sealed class CameraServiceStub : ICameraService
        {
            public string Id => nameof(CameraServiceStub);
            public Vector3 CameraPosition => Vector3.zero;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position) => position;
            public RaycastHit ScreenPointToRay(Vector2 screenPoint) => default;
            public Vector3 WorldToViewportPoint(Vector3 currentPosition) => currentPosition;
            public Vector2 WorldToScreenPoint(Vector3 position) => Vector2.zero;
            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 mapMin, Vector2 mapMax) => Array.Empty<Vector3>();
            public void MoveTo(Vector3 worldPoint) { }
        }
    }
}
