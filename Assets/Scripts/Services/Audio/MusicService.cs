using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Audio
{
    public sealed class MusicService : Service, IMusicService, IInitializable, ITickable, ILateDisposable
    {
        private const float MUSIC_FADE_DURATION = 1f;

        private const string SOURCE_PATH = "MusicSource";

        private readonly ISceneService _sceneService;
        private readonly IGameModelObserver _gameModelObserver;
        private readonly IAudioService _audioService;

        private readonly MusicAudioData _musicAudioData;
        private readonly AudioSource _audioSource;
        private readonly System.Random _random = new System.Random();
        private List<AudioClip> _clips;

        private readonly float _volume;
        private float _fadeDuration;

        private bool _playing;
        private bool _fadingOut;

        public MusicService(ISceneService sceneService, IAssetService assetService, IGameModelObserver gameModelObserver, IAudioService audioService)
        {
            _sceneService = sceneService;
            _gameModelObserver = gameModelObserver;
            _audioService = audioService;
            _musicAudioData = assetService.Load<MusicAudioData>(nameof(MusicAudioData));
            _audioSource = Object.Instantiate(assetService.LoadComponent<AudioSource>(SOURCE_PATH));
            _audioSource.ignoreListenerPause = true;
            _audioSource.priority = 16;
            _volume = _audioSource.volume;
            Object.DontDestroyOnLoad(_audioSource.gameObject);
        }

        public void Initialize()
        {
            OnSceneLoad(_sceneService.TargetScene);
            _sceneService.OnSceneActivation += OnSceneLoad;
        }

        public void LateDispose()
        {
            // The project context only disposes on quit or Play Mode exit, when Unity also destroys the
            // DontDestroyOnLoad source in no guaranteed order; touching it here can hit a destroyed object.
            _sceneService.OnSceneActivation -= OnSceneLoad;
        }

        private void OnSceneLoad(SceneType scene)
        {
            if (scene == SceneType.Loading) return;
            FactionType faction = scene == SceneType.MainMenu
                ? default : MatchRules.FindHuman(_gameModelObserver.Players).Faction;
            _clips = _musicAudioData.GetMusicList(scene, faction);
            if (_playing) _fadingOut = true;
            else PlayNext();
        }

        private void PlayNext()
        {
            _audioService.Stop(_audioSource);
            _playing = false;
            _fadingOut = false;
            if (_clips.Count == 0) return;
            AudioClip clip = _clips[_random.Next(_clips.Count)];
            _fadeDuration = Mathf.Min(MUSIC_FADE_DURATION, clip.length * 0.5f);
            _audioService.Play(_audioSource, clip, 0f, false);
            _playing = true;
        }

        public void Tick()
        {
            if (!_playing) return;
            if (!_audioSource.isPlaying)
            {
                PlayNext();
                return;
            }
            AudioClip clip = _audioSource.clip;
            float remaining = (clip.samples - _audioSource.timeSamples) / (float)clip.frequency;
            if (remaining <= _fadeDuration) _fadingOut = true;
            _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, _fadingOut ? 0f : _volume,
                _volume * Time.unscaledDeltaTime / _fadeDuration);
            if (_fadingOut && _audioSource.volume == 0f) PlayNext();
        }
    }
}
