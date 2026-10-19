using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleSaveRepositoryTests
    {
        private const int MAP_SEED = 4242;

        private string _directory;
        private JsonBattleSaveRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "BattleSaveRepositoryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _repository = new JsonBattleSaveRepository(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [Test]
        public void Save_ThenLoad_RoundTripsHeaderSetupAndCamera()
        {
            DateTime savedAt = new DateTime(2026, 10, 19, 12, 30, 0, DateTimeKind.Utc);
            BattleSave save = CreateSave(Skirmish(), BattleSaveSlot.Quicksave, savedAt);

            _repository.Save(save);
            BattleSave loaded = _repository.Load(GameMode.Skirmish, BattleSaveSlot.Quicksave);

            Assert.That(File.Exists(Path.Combine(_directory, "Saves", "Skirmish", "quicksave.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(_directory, "Saves", "Skirmish", "quicksave.json.tmp")), Is.False);
            Assert.That(loaded.Header.Version, Is.EqualTo(BattleSaveHeader.CURRENT_VERSION));
            Assert.That(loaded.Header.Mode, Is.EqualTo(GameMode.Skirmish));
            Assert.That(loaded.Header.Slot.Kind, Is.EqualTo(BattleSaveSlotKind.Quicksave));
            Assert.That(loaded.Header.SavedAtUtc, Is.EqualTo(savedAt));
            Assert.That(loaded.Header.BattleTime, Is.EqualTo(312.5f));
            Assert.That(loaded.Header.GameSpeed, Is.EqualTo(GameSpeed.Fast));
            Assert.That(loaded.Camera.Position, Is.EqualTo(new Vector3(10f, 450f, -30f)));
            Assert.That(Quaternion.Angle(loaded.Camera.Rotation, Quaternion.Euler(55f, 20f, 0f)), Is.LessThan(0.01f));
            Assert.That(loaded.Camera.ZoomTargetHeight, Is.EqualTo(380f));
            BattleDefinition rebuilt = loaded.Setup.Rebuild();
            Assert.That(rebuilt.Mode, Is.EqualTo(GameMode.Skirmish));
            Assert.That(rebuilt.Map.Seed, Is.EqualTo(MAP_SEED));
        }

        [Test]
        public void Save_OverSameSlot_ReplacesPreviousSave()
        {
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Autosave, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            DateTime later = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Autosave, later));

            Assert.That(_repository.Load(GameMode.Skirmish, BattleSaveSlot.Autosave).Header.SavedAtUtc, Is.EqualTo(later));
            Assert.That(_repository.List(GameMode.Skirmish).Count, Is.EqualTo(1));
        }

        [Test]
        public void ModesUseSeparateFolders()
        {
            _repository.Save(CreateSave(Survival(), BattleSaveSlot.Quicksave, DateTime.UtcNow));

            Assert.That(File.Exists(Path.Combine(_directory, "Saves", "Survival", "quicksave.json")), Is.True);
            Assert.That(_repository.List(GameMode.Skirmish), Is.Empty);
            Assert.Throws<FileNotFoundException>(() => _repository.Load(GameMode.Skirmish, BattleSaveSlot.Quicksave));
            Assert.That(_repository.Load(GameMode.Survival, BattleSaveSlot.Quicksave).Setup.Rebuild().Mode,
                Is.EqualTo(GameMode.Survival));
        }

        [Test]
        public void Load_SaveOfAnotherModeInFolder_IsRefused()
        {
            _repository.Save(CreateSave(Survival(), BattleSaveSlot.Quicksave, DateTime.UtcNow));
            string skirmishFolder = Path.Combine(_directory, "Saves", "Skirmish");
            Directory.CreateDirectory(skirmishFolder);
            File.Copy(Path.Combine(_directory, "Saves", "Survival", "quicksave.json"), Path.Combine(skirmishFolder, "quicksave.json"));

            Assert.Throws<InvalidDataException>(() => _repository.Load(GameMode.Skirmish, BattleSaveSlot.Quicksave));
        }

        [Test]
        public void Load_OlderVersion_IsRefusedButListed()
        {
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Quicksave, DateTime.UtcNow));
            string path = Path.Combine(_directory, "Saves", "Skirmish", "quicksave.json");
            File.WriteAllText(path, Regex.Replace(File.ReadAllText(path), "\"version\":\\s*\\d+", "\"version\": 0"));

            InvalidDataException error = Assert.Throws<InvalidDataException>(
                () => _repository.Load(GameMode.Skirmish, BattleSaveSlot.Quicksave));
            Assert.That(error.Message, Does.Contain("version 0"));
            Assert.That(_repository.List(GameMode.Skirmish).Single().IsCompatible, Is.False);
        }

        [Test]
        public void CorruptFile_LoadThrowsAndListSkipsIt()
        {
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Named("Good"), DateTime.UtcNow));
            File.WriteAllText(Path.Combine(_directory, "Saves", "Skirmish", "Broken.json"), "{ not json");

            Assert.Throws<InvalidDataException>(() => _repository.Load(GameMode.Skirmish, BattleSaveSlot.Named("Broken")));
            LogAssert.Expect(LogType.Warning, new Regex("Skipping unreadable save"));
            Assert.That(_repository.List(GameMode.Skirmish).Select(header => header.Slot.Name), Is.EqualTo(new[] { "Good" }));
        }

        [Test]
        public void List_ReturnsNewestFirst()
        {
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Named("Old"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Named("New"), new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
            _repository.Save(CreateSave(Skirmish(), BattleSaveSlot.Quicksave, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

            Assert.That(_repository.List(GameMode.Skirmish).Select(header => header.Slot.Name),
                Is.EqualTo(new[] { "New", "quicksave", "Old" }));
        }

        [Test]
        public void Simulation_HasNoSaveFolder()
        {
            Assert.Throws<ArgumentException>(() => _repository.List(GameMode.BattleSimulation));
            Assert.Throws<ArgumentException>(() => _repository.Load(GameMode.BattleSimulation, BattleSaveSlot.Quicksave));
        }

        [TestCase("")]
        [TestCase(" padded")]
        [TestCase("../escape")]
        [TestCase("a:b")]
        [TestCase("trailing.")]
        [TestCase("autosave")]
        [TestCase("QuickSave")]
        public void NamedSlot_RejectsInvalidOrReservedNames(string name)
        {
            Assert.Throws<ArgumentException>(() => BattleSaveSlot.Named(name));
        }

        [Test]
        public void SlotFromName_MapsReservedNamesToTheirKinds()
        {
            Assert.That(BattleSaveSlot.FromName("autosave"), Is.SameAs(BattleSaveSlot.Autosave));
            Assert.That(BattleSaveSlot.FromName("quicksave"), Is.SameAs(BattleSaveSlot.Quicksave));
            Assert.That(BattleSaveSlot.FromName("Before the push").Kind, Is.EqualTo(BattleSaveSlotKind.Named));
        }

        private static BattleSave CreateSave(BattleDefinition battle, BattleSaveSlot slot, DateTime savedAt) =>
            BattleSave.Create(BattleSaveHeader.Create(battle.Mode, slot, savedAt, 312.5f, GameSpeed.Fast),
                BattleSetupSave.From(battle),
                CameraPoseSave.Create(new Vector3(10f, 450f, -30f), Quaternion.Euler(55f, 20f, 0f), 380f),
                Array.Empty<UnitSave>(), Array.Empty<StructureSave>());

        private static BattleDefinition Skirmish() => SkirmishPreset.Create(TestPlayers.CreateDuel().Players,
            PlanetType.Coruscant, MapSize.Small, BattleVictoryCondition.DestroyEnemyFleet, 1000f, true, MAP_SEED).Build();

        private static BattleDefinition Survival() => SurvivalPreset.Create(new SurvivalSettings(FactionType.Empire,
            null, FactionType.Rebellion, MapSize.Small, EnemyAiDifficulty.Medium, true)).Build();
    }
}
