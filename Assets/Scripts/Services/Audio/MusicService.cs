using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Audio
{
    public sealed class MusicService : Service, IMusicService, IInitializable, ITickable, ILateDisposable
    {
        private const float MUSIC_FADE_DURATION = 1f;
        private const float VOICE_DUCK_GAIN = 0.2f;

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
        private float _fadeVolume;
        private float _duckGain = 1f;

        private bool _playing;
        private bool _started;
        private bool _fadingOut;

        public MusicService(ISceneService sceneService, IAssetService assetService, IGameModelObserver gameModelObserver,
            IAudioService audioService, AudioMixer mixer)
        {
            _sceneService = sceneService;
            _gameModelObserver = gameModelObserver;
            _audioService = audioService;
            _musicAudioData = assetService.Load<MusicAudioData>(nameof(MusicAudioData));
            _audioSource = Object.Instantiate(assetService.LoadComponent<AudioSource>(SOURCE_PATH));
            _audioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Music").Single();
            _audioSource.ignoreListenerPause = true;
            _audioSource.priority = 224;
            _volume = _audioSource.volume;
            if (Application.isPlaying) Object.DontDestroyOnLoad(_audioSource.gameObject);
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
            if (!Application.isPlaying) Object.DestroyImmediate(_audioSource.gameObject);
        }

        private void OnSceneLoad(SceneType scene)
        {
            if (scene == SceneType.Loading) return;
            FactionType faction = scene == SceneType.MainMenu
                ? default : _gameModelObserver.GameMode == GameMode.BattleSimulation
                    ? _gameModelObserver.Players[0].Faction : MatchRules.FindHuman(_gameModelObserver.Players).Faction;
            _clips = _musicAudioData.GetMusicList(scene, faction);
            if (_playing) _fadingOut = true;
            else PlayNext();
        }

        private void PlayNext()
        {
            _audioService.Stop(_audioSource);
            _playing = false;
            _started = false;
            _fadingOut = false;
            if (_clips.Count == 0) return;
            AudioClip clip = _clips[_random.Next(_clips.Count)];
            _fadeDuration = Mathf.Min(MUSIC_FADE_DURATION, clip.length * 0.5f);
            _audioService.Play(_audioSource, clip, 0f, false);
            _fadeVolume = 0f;
            _playing = true;
        }

        public void SetVoiceDucking(bool active)
        {
            _duckGain = active ? VOICE_DUCK_GAIN : 1f;
            _audioSource.volume = _fadeVolume * _duckGain;
        }

        public void Tick()
        {
            if (!_playing) return;
            AudioClip clip = _audioSource.clip;
            if (clip.loadState == AudioDataLoadState.Failed)
                throw new System.InvalidOperationException($"Failed to load music clip '{clip.name}'.");
            // Only a deferred Play waits for audio data. The player releases a finished stream's data, so a
            // started track that reports Unloaded has ended and must advance instead of stalling here.
            if (!_started)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Loading) return;
                _started = true;
            }
            if (!_audioSource.isPlaying)
            {
                PlayNext();
                return;
            }
            float remaining = (clip.samples - _audioSource.timeSamples) / (float)clip.frequency;
            if (remaining <= _fadeDuration) _fadingOut = true;
            _fadeVolume = Mathf.MoveTowards(_fadeVolume, _fadingOut ? 0f : _volume,
                _volume * Time.unscaledDeltaTime / _fadeDuration);
            _audioSource.volume = _fadeVolume * _duckGain;
            if (_fadingOut && _fadeVolume == 0f) PlayNext();
        }
    }
}
