using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class GameControllerTests
    {
        private GameModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = ScriptableObject.CreateInstance<GameModel>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_model);
        }

        [Test]
        public void StartGame_WithSameFaction_RejectsMatchBeforeLoadingScene()
        {
            TrackingSceneService sceneService = new TrackingSceneService();
            GameController controller = new GameController(_model, sceneService);

            Assert.Throws<ArgumentException>(() => controller.StartGame(
                FactionType.Republic,
                FactionType.Republic,
                PlanetType.Coruscant,
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Medium,
                2000f));
            Assert.That(sceneService.LoadRequestCount, Is.Zero);
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

            public void LoadSceneByPlanetType(PlanetType planetType)
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
