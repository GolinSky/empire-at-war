using System;
using System.IO;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.SceneService;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
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
            GameController controller = new GameController(model: _model, sceneService: sceneService);
            PlayerSlot[] sameTeam =
            {
                CreateSlot(0, 0, PlayerController.Human),
                CreateSlot(1, 0, PlayerController.Ai)
            };

            Assert.Throws<ArgumentException>(() => controller.StartBattle(SkirmishPreset.Create(
                sameTeam,
                PlanetType.Coruscant,
                MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet,
                2000f,
                true,
                0).Build()));
            Assert.That(sceneService.LoadRequestCount, Is.Zero);
        }

        [Test]
        public void StartGame_TwoVersusTwoMirrorMatch_StoresPlayersAndLoadsBattle()
        {
            TrackingSceneService sceneService = new TrackingSceneService();
            GameController controller = new GameController(model: _model, sceneService: sceneService);
            PlayerSlot[] players =
            {
                CreateSlot(0, 0, PlayerController.Human),
                CreateSlot(1, 1, PlayerController.Ai),
                CreateSlot(2, 0, PlayerController.Ai),
                CreateSlot(3, 1, PlayerController.Ai)
            };

            controller.StartBattle(SkirmishPreset.Create(
                players,
                PlanetType.Coruscant,
                MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet,
                2000f,
                true,
                0).Build());

            Assert.That(_model.Players, Is.EqualTo(players));
            Assert.That(sceneService.LoadRequestCount, Is.EqualTo(1));
        }

        [Test]
        public void LoadBattle_RebuildsDefinitionAndKeepsSnapshotUntilStartup()
        {
            var scenes = new TrackingSceneService();
            var controller = new GameController(scenes, _model);
            var definition = SkirmishPreset.Default();
            var save = EmpireAtWar.Services.Saves.BattleSave.Create(
                EmpireAtWar.Services.Saves.BattleSaveHeader.Create(GameMode.Skirmish,
                    EmpireAtWar.Services.Saves.BattleSaveSlot.Quicksave, DateTime.UtcNow, 456f,
                    EmpireAtWar.Controllers.Game.GameSpeed.Fast), BattleSetupSave.From(definition),
                EmpireAtWar.Services.Saves.CameraPoseSave.Create(Vector3.zero, Quaternion.identity, 600f),
                Array.Empty<EmpireAtWar.Services.Saves.UnitSave>(), Array.Empty<EmpireAtWar.Services.Saves.StructureSave>());

            controller.LoadBattle(save);
            Assert.That(_model.Battle.Map.Seed, Is.EqualTo(definition.Map.Seed));
            Assert.That(_model.PendingBattleSave, Is.SameAs(save));
            Assert.That(scenes.LoadRequestCount, Is.EqualTo(1));
            controller.StartBattle(definition);
            Assert.That(_model.PendingBattleSave, Is.Null);
            controller.LoadBattle(save);
            controller.ExitGame();
            Assert.That(_model.PendingBattleSave, Is.Null);
        }

        [Test]
        public void LoadBattle_WithIncompatibleSaveContent_ThrowsBeforeMutatingModelOrLoadingScene()
        {
            var scenes = new TrackingSceneService();
            using var assets = new CachedAssetService(new AddressableHandleLoader());
            var mapSettings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            Assert.That(mapSettings, Is.Not.Null);
            var validator = new BattleSaveValidator(assets, mapSettings);
            var controller = new GameController(scenes, _model, validator);
            var definition = SkirmishPreset.Default();
            var badUnit = UnitSave.Create(1L, new PlayerId(0), UnitTypeId.Ship(ShipType.Venator),
                Vector3.zero, Quaternion.identity,
                UnitHealthSave.ForShip(1f, 1f, new[] { 1f }),
                UnitOrderSave.From(new UnitOrderModel()));
            var save = BattleSave.Create(
                BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Quicksave, DateTime.UtcNow, 10f,
                    EmpireAtWar.Controllers.Game.GameSpeed.Normal),
                BattleSetupSave.From(definition),
                CameraPoseSave.Create(Vector3.zero, Quaternion.identity, 600f),
                new[] { badUnit }, Array.Empty<StructureSave>());

            Assert.Throws<InvalidDataException>(() => controller.LoadBattle(save));
            Assert.That(_model.PendingBattleSave, Is.Null);
            Assert.That(scenes.LoadRequestCount, Is.Zero);
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
