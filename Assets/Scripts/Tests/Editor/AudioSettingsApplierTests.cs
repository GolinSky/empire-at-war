using System.Collections;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AudioSettingsApplierTests
    {
        private AudioMixer _mixer;
        private AudioSettingsApplier _applier;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();
            _mixer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer"));
            _applier = new AudioSettingsApplier(_mixer);
            _applier.Initialize();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _applier.Dispose();
            Object.DestroyImmediate(_mixer);
            yield return new ExitPlayMode();
        }

        [Test]
        public void Startup_DefersSavedMuteUntilFirstTick()
        {
            Assert.That(_mixer.GetFloat("MasterVolume", out float authored), Is.True);
            SettingsData saved = new SettingsData();
            saved.Audio.MasterVolume = 0f;
            saved.Audio.MuteWhenUnfocused = false;

            _applier.Apply(saved);

            Assert.That(_mixer.GetFloat("MasterVolume", out float beforeTick), Is.True);
            Assert.That(beforeTick, Is.EqualTo(authored), "Startup must not write the mixer during Awake.");
            _applier.Tick();
            Assert.That(_mixer.GetFloat("MasterVolume", out float afterTick), Is.True);
            Assert.That(afterTick, Is.EqualTo(-80f));

            saved.Audio.MasterVolume = 1f;
            _applier.Preview(saved.Audio);
            Assert.That(_mixer.GetFloat("MasterVolume", out float restored), Is.True);
            Assert.That(restored, Is.EqualTo(authored).Within(0.001f));
        }

        [Test]
        public void Startup_UsesLatestPreviewAndPreservesAuthoredMix()
        {
            Assert.That(_mixer.GetFloat("MusicVolume", out float authored), Is.True);
            _applier.Preview(new AudioSettingsData { MusicVolume = 0f, MuteWhenUnfocused = false });
            _applier.Preview(new AudioSettingsData { MusicVolume = 0.5f, MuteWhenUnfocused = false });

            _applier.Tick();
            Assert.That(_mixer.GetFloat("MusicVolume", out float volume), Is.True);
            Assert.That(volume, Is.EqualTo(authored + 20f * Mathf.Log10(0.5f)).Within(0.001f));

            _applier.Tick();
            Assert.That(_mixer.GetFloat("MusicVolume", out float nextTick), Is.True);
            Assert.That(nextTick, Is.EqualTo(volume));
        }
    }
}
