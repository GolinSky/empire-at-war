using System;
using System.IO;
using System.Text.RegularExpressions;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class JsonSettingsRepositoryTests
    {
        private const string FILE_NAME = "settings.json";

        private JsonSettingsRepository _repository;

        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SettingsRepositoryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, FILE_NAME);
            _repository = new JsonSettingsRepository(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [Test]
        public void Load_WithoutFile_ReturnsMissing()
        {
            Assert.That(_repository.Load(out SettingsData data), Is.EqualTo(SettingsLoadStatus.Missing));
            Assert.That(data, Is.Null);
        }

        [Test]
        public void Save_ThenLoad_RoundTrips()
        {
            SettingsData saved = new SettingsData();
            saved.Audio.MusicVolume = 0.4f;

            _repository.Save(saved);

            Assert.That(_repository.Load(out SettingsData loaded), Is.EqualTo(SettingsLoadStatus.Loaded));
            Assert.That(loaded.Matches(saved), Is.True);
            Assert.That(File.Exists(_path + ".tmp"), Is.False);
        }

        [Test]
        public void Save_OverExistingFile_KeepsPreviousAsBackup()
        {
            SettingsData first = new SettingsData();
            first.Camera.PanSpeedMultiplier = 2f;
            _repository.Save(first);

            _repository.Save(new SettingsData());

            SettingsData backup = SettingsData.FromJson(File.ReadAllText(_path + ".bak"));
            Assert.That(backup.Camera.PanSpeedMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void Load_CorruptFile_LoadsBackupAndKeepsCorruptCopy()
        {
            SettingsData good = new SettingsData();
            good.Audio.SfxVolume = 0.3f;
            _repository.Save(good);
            _repository.Save(good);
            File.WriteAllText(_path, "{ not json");
            LogAssert.Expect(LogType.Warning, new Regex("unreadable"));

            Assert.That(_repository.Load(out SettingsData loaded), Is.EqualTo(SettingsLoadStatus.Loaded));
            Assert.That(loaded.Audio.SfxVolume, Is.EqualTo(0.3f));
            Assert.That(File.ReadAllText(_path + ".corrupt"), Is.EqualTo("{ not json"));
        }

        [Test]
        public void Load_CorruptFileWithoutBackup_ReturnsUnreadableAndLeavesFile()
        {
            File.WriteAllText(_path, "{ not json");
            LogAssert.Expect(LogType.Warning, new Regex("unreadable"));

            Assert.That(_repository.Load(out SettingsData _), Is.EqualTo(SettingsLoadStatus.Unreadable));
            Assert.That(File.ReadAllText(_path), Is.EqualTo("{ not json"));
        }

        [Test]
        public void Load_LockedFile_ReturnsUnreadableInsteadOfThrowing()
        {
            File.WriteAllText(_path, new SettingsData().ToJson());
            using (new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                LogAssert.Expect(LogType.Warning, new Regex("unreadable"));
                LogAssert.Expect(LogType.Warning, new Regex("Could not copy"));

                Assert.That(_repository.Load(out SettingsData _), Is.EqualTo(SettingsLoadStatus.Unreadable));
            }
        }
    }
}
