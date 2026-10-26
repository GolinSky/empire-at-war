using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("MusicReplacement")]
    public sealed class MusicServiceTests
    {
        private AudioClip _clip;
        private AudioSource _source;
        private MusicService _service;
        private RecordingAudioService _audio;

        [TearDown]
        public void TearDown()
        {
            if (_source != null) Object.DestroyImmediate(_source.gameObject);
            if (_clip != null) Object.DestroyImmediate(_clip);
        }

        [UnityTearDown]
        public IEnumerator ExitRuntime()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Tick_AdvancesBetweenStreamedBattleTracks()
        {
            yield return new EnterPlayMode();
            AudioClip first = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Music/Battle/Ahsoka/A_Floating.mp3");
            StartTrack(first, true);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_source.volume < 0.5f && Time.realtimeSinceStartup < deadline)
            {
                _service.Tick();
                yield return null;
            }
            Assert.That(_source.volume, Is.GreaterThanOrEqualTo(0.5f), $"First track: {first.loadState}");
            AudioClip next = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Music/Battle/Ahsoka/A_Open_Fire.mp3");
            Assert.That(next.UnloadAudioData(), Is.True);
            typeof(MusicService).GetField("_clips", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, new List<AudioClip> { next });
            _source.timeSamples = first.samples - first.frequency / 4;
            deadline = Time.realtimeSinceStartup + 10f;
            while ((_audio.PlayCount < 2 || _source.volume < 0.5f) && Time.realtimeSinceStartup < deadline)
            {
                _service.Tick();
                yield return null;
            }
            Assert.That(_audio.PlayCount, Is.EqualTo(2));
            Assert.That(_source.clip, Is.SameAs(next));
            Assert.That(_source.isPlaying, Is.True);
            Assert.That(_source.volume, Is.GreaterThanOrEqualTo(0.5f), $"Next track: {_source.clip.loadState}");
            Assert.That(_source.timeSamples, Is.GreaterThan(0));
        }

        [Test]
        public void Constructor_RoutesMusicToInjectedMixerInsteadOfPrefabMixer()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer");
            string mixerPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Sandbox/MusicServiceTestMixer.mixer");
            GameObject template = new GameObject("Bundled music source template");
            AudioSource source = template.AddComponent<AudioSource>();
            source.playOnAwake = false;
            try
            {
                Assert.That(AssetDatabase.CopyAsset("Assets/Audio/AudioMixer.mixer", mixerPath), Is.True);
                AssetDatabase.SaveAssets();
                AudioMixer bundledMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
                source.outputAudioMixerGroup = bundledMixer.FindMatchingGroups("Music").Single();
                Assert.That(source.outputAudioMixerGroup.audioMixer, Is.Not.SameAs(mixer));

                _service = new MusicService(null, new SourceAssets(source), null, new RecordingAudioService(), mixer);
                _source = (AudioSource)typeof(MusicService).GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_service);

                Assert.That(_source.outputAudioMixerGroup, Is.SameAs(mixer.FindMatchingGroups("Music").Single()));
                Assert.That(_source.outputAudioMixerGroup.audioMixer, Is.SameAs(mixer));
                Assert.That(source.outputAudioMixerGroup.audioMixer, Is.SameAs(bundledMixer));
            }
            finally
            {
                Object.DestroyImmediate(template);
                AssetDatabase.DeleteAsset(mixerPath);
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void VoiceDuckingPreservesMusicFadeLevelAndRestoresIt()
        {
            _clip = AudioClip.Create("Music duck", 44100, 1, 44100, false);
            StartTrack(_clip);
            typeof(MusicService).GetField("_fadeVolume", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, 0.6f);
            _service.SetVoiceDucking(true);
            Assert.That(_source.volume, Is.EqualTo(0.12f).Within(0.001f));
            _service.SetVoiceDucking(false);
            Assert.That(_source.volume, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(_source.priority, Is.GreaterThan(128));
        }

        [Test]
        public void Tick_DoesNotReplaceClipWhilePlaybackIsWaitingForAudioData()
        {
            AudioClip importedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Music/Battle/Ahsoka/A_Floating.mp3");
            Assert.That(importedClip, Is.Not.Null);
            Assert.That(importedClip.UnloadAudioData(), Is.True);
            Assert.That(importedClip.loadState, Is.EqualTo(AudioDataLoadState.Unloaded));
            StartTrack(importedClip);

            for (int i = 0; i < 10; i++) _service.Tick();

            Assert.That(_audio.PlayCount, Is.EqualTo(1), "A deferred Play request must keep its selected clip.");
            Assert.That(_source.clip, Is.SameAs(importedClip));
        }

        [Test]
        public void Tick_SelectsNextTrackAfterStartedStreamReleasesAudioData()
        {
            AudioClip importedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Music/Battle/Ahsoka/A_Floating.mp3");
            StartTrack(importedClip);
            typeof(MusicService).GetField("_started", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_service, true);
            Assert.That(importedClip.UnloadAudioData(), Is.True);
            Assert.That(_source.isPlaying, Is.False);

            _service.Tick();

            Assert.That(_audio.PlayCount, Is.EqualTo(2), "A finished stream must not stall the playlist.");
        }

        [Test]
        public void Tick_SelectsNextTrackAfterLoadedPlaybackStops()
        {
            _clip = AudioClip.Create("Finished music", 44100, 1, 44100, false);
            Assert.That(_clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
            StartTrack(_clip);
            Assert.That(_source.isPlaying, Is.False);

            _service.Tick();

            Assert.That(_audio.PlayCount, Is.EqualTo(2));
        }

        private void StartTrack(AudioClip clip, bool realPlayback = false)
        {
            GameObject template = new GameObject("Music source template");
            AudioSource source = template.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _audio = new RecordingAudioService { RealPlayback = realPlayback };
            try
            {
                _service = new MusicService(null, new SourceAssets(source), null, _audio,
                    AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer"));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
            _source = (AudioSource)typeof(MusicService).GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_service);
            typeof(MusicService).GetField("_clips", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_service, new List<AudioClip> { clip });
            typeof(MusicService).GetMethod("PlayNext", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_service, null);
        }

        private sealed class SourceAssets : IAssetService
        {
            private readonly AudioSource _source;
            public SourceAssets(AudioSource source) => _source = source;
            public TSource Load<TSource>(string key) where TSource : Object =>
                AssetDatabase.LoadAssetAtPath<TSource>("Assets/Settings/Data/Models/Audio/MusicAudioData.asset");
            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component => (TComponent)(Component)_source;
            public GameObject LoadPrefab(string key) => throw new NotSupportedException();
        }

        private sealed class RecordingAudioService : Service, IAudioService
        {
            public bool RealPlayback { get; set; }
            public int PlayCount { get; private set; }
            public void Play(AudioSource source, AudioClip clip, float volume, bool loop)
            {
                PlayCount++;
                source.clip = clip;
                source.volume = volume;
                source.loop = loop;
                if (RealPlayback) source.Play();
            }
            public void Stop(AudioSource source) => source.Stop();
            public void PlayOneShot(AudioSource source, AudioClip clip, float volume) => throw new NotSupportedException();
            public void Pause(AudioSource source) => throw new NotSupportedException();
            public void UnPause(AudioSource source) => throw new NotSupportedException();
            public void SetGamePaused(bool paused) => throw new NotSupportedException();
        }
    }
}
