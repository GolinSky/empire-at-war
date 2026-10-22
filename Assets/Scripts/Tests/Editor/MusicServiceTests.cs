using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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
        public void Tick_SelectsNextTrackAfterLoadedPlaybackStops()
        {
            _clip = AudioClip.Create("Finished music", 44100, 1, 44100, false);
            Assert.That(_clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
            StartTrack(_clip);
            Assert.That(_source.isPlaying, Is.False);

            _service.Tick();

            Assert.That(_audio.PlayCount, Is.EqualTo(2));
        }

        private void StartTrack(AudioClip clip)
        {
            GameObject template = new GameObject("Music source template");
            AudioSource source = template.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _audio = new RecordingAudioService();
            try
            {
                _service = new MusicService(null, new SourceAssets(source), null, _audio);
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
            public int PlayCount { get; private set; }
            public void Play(AudioSource source, AudioClip clip, float volume, bool loop)
            {
                PlayCount++;
                source.clip = clip;
                source.volume = volume;
                source.loop = loop;
            }
            public void Stop(AudioSource source) => source.Stop();
            public void PlayOneShot(AudioSource source, AudioClip clip, float volume) => throw new NotSupportedException();
            public void Pause(AudioSource source) => throw new NotSupportedException();
            public void UnPause(AudioSource source) => throw new NotSupportedException();
            public void SetGamePaused(bool paused) => throw new NotSupportedException();
        }
    }
}
