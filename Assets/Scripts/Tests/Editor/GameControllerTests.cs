using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class GameControllerTests
    {
        private GameData _model;

        [SetUp]
        public void SetUp()
        {
            _model = ScriptableObject.CreateInstance<GameData>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_model);
        }

        [Test]
        public void StartGame_WithoutOpposingTeam_RejectsMatchBeforeLoadingScene()
        {
            TrackingSceneService sceneService = new TrackingSceneService();
            GameController controller = new GameController(_model, sceneService);
            PlayerSlot[] sameTeam =
            {
                CreateSlot(0, 0, PlayerController.Human),
                CreateSlot(1, 0, PlayerController.Ai)
            };

            Assert.Throws<ArgumentException>(() => controller.StartGame(
                sameTeam,
                PlanetType.Coruscant,
                MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet,
                2000f));
            Assert.That(sceneService.LoadRequestCount, Is.Zero);
        }

        [Test]
        public void StartGame_TwoVersusTwoMirrorMatch_StoresPlayersAndLoadsBattle()
        {
            TrackingSceneService sceneService = new TrackingSceneService();
            GameController controller = new GameController(_model, sceneService);
            PlayerSlot[] players =
            {
                CreateSlot(0, 0, PlayerController.Human),
                CreateSlot(1, 1, PlayerController.Ai),
                CreateSlot(2, 0, PlayerController.Ai),
                CreateSlot(3, 1, PlayerController.Ai)
            };

            controller.StartGame(
                players,
                PlanetType.Coruscant,
                MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet,
                2000f);

            Assert.That(_model.Players, Is.SameAs(players));
            Assert.That(sceneService.LoadRequestCount, Is.EqualTo(1));
        }

        // Every slot plays the same faction: mirror matches are allowed now that stations are keyed by player.
        private static PlayerSlot CreateSlot(int index, int team, PlayerController controller)
        {
            return new PlayerSlot(new PlayerId(index), new TeamId(team), FactionType.Republic, controller,
                EnemyAiDifficulty.Medium, index);
        }

        private sealed class TrackingSceneService : ISceneService
        {
            public event Action<SceneType> OnSceneActivation;

            public string Id => nameof(TrackingSceneService);
            public SceneType TargetScene => default;
            public bool IsSceneLoaded => false;
            public int LoadRequestCount { get; private set; }

            public void LoadScene(SceneType sceneType)
            {
                LoadRequestCount++;
            }

            public void ActivateScene()
            {
                OnSceneActivation?.Invoke(TargetScene);
            }
        }
    }
}
