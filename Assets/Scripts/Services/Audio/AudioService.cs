using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using EmpireAtWar.Mvc;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.Services.Audio
{
    public interface IAudioService:IService
    {
        void PlayOneShot(AudioClip audioClip, AudioType audioType);
        bool CanPlayAlarm();
        void RegisterAlarmPlaying();
    }

    public class AudioService: Service, IInitializable, ILateDisposable, ITickable, IAudioService
    {
        private const float SOUND_DELAY = 2f;
        private const float MUSIC_FADE_DURATION = 1f;
        private const string SOURCE_PATH = "MusicSource";
        private const string DIALOG_SOURCE_PATH = "AudioDialogSource";
        private readonly ISceneService _sceneService;
        private readonly IGameModelObserver _gameModelObserver;
        private readonly MusicAudioData _musicAudioModel;
        private readonly AudioSource _backgroundSource;
        private readonly AudioSource _dialogSource;
        private readonly Random _random;
        private readonly float _musicVolume;
        private List<AudioClip> _clips;
        private bool _isMusicPlaying;
        private bool _isFadingOut;
        private float _musicFadeDuration;
        private float _lastTimePlayAlarm;
        private float _lastTimePlaySfx;
        
       
        public AudioService(ISceneService sceneService, IAssetService repository, IGameModelObserver gameModelObserver)
        {
            _sceneService = sceneService;
            _gameModelObserver = gameModelObserver;
            _musicAudioModel = repository.Load<MusicAudioData>(nameof(MusicAudioData));
            _backgroundSource = Object.Instantiate(repository.LoadComponent<AudioSource>(SOURCE_PATH));
            _musicVolume = _backgroundSource.volume;
            _dialogSource = Object.Instantiate(repository.LoadComponent<AudioSource>(DIALOG_SOURCE_PATH));
            Object.DontDestroyOnLoad(_backgroundSource);
            Object.DontDestroyOnLoad(_dialogSource);
            _random = new Random();
        }
        
        public void Initialize()
        {
            OnSceneLoad(_sceneService.TargetScene);
            _sceneService.OnSceneActivation += OnSceneLoad;
        }
        
        public void LateDispose()
        {
            _sceneService.OnSceneActivation -= OnSceneLoad;
        }
        
        private void OnSceneLoad(SceneType sceneType)
        {
            if(sceneType == SceneType.Loading) return;
            
            PlayMusic(sceneType);
        }

        private void PlayMusic(SceneType sceneType)
        {
            _clips = _musicAudioModel.GetMusicList(sceneType, _gameModelObserver.PlayerFactionType);
            if (_isMusicPlaying)
            {
                _isFadingOut = true;
                return;
            }

            PlayMusicInternal();
        }

        private void PlayMusicInternal()
        {
            _backgroundSource.Stop();
            _isMusicPlaying = false;
            _isFadingOut = false;
            if(_clips == null || _clips.Count == 0) return;
            int randomIndex = _random.Next(_clips.Count);
            AudioClip audioClip = _clips.ElementAt(randomIndex);
            _backgroundSource.clip = audioClip;
            _backgroundSource.volume = 0f;
            _musicFadeDuration = Mathf.Min(MUSIC_FADE_DURATION, audioClip.length * 0.5f);
            _backgroundSource.Play();
            _isMusicPlaying = true;
        }

        public void Tick()
        {
            if(!_isMusicPlaying) return;

            if (!_backgroundSource.isPlaying)
            {
                PlayMusicInternal();
                return;
            }

            AudioClip audioClip = _backgroundSource.clip;
            float remainingTime = (audioClip.samples - _backgroundSource.timeSamples) / (float)audioClip.frequency;
            if (remainingTime <= _musicFadeDuration)
            {
                _isFadingOut = true;
            }

            float targetVolume = _isFadingOut ? 0f : _musicVolume;
            _backgroundSource.volume = Mathf.MoveTowards(_backgroundSource.volume, targetVolume,
                _musicVolume * Time.unscaledDeltaTime / _musicFadeDuration);

            if (_isFadingOut && _backgroundSource.volume == 0f)
            {
                PlayMusicInternal();
            }
        }

        public void PlayOneShot(AudioClip audioClip, AudioType audioType)
        {
            if(_lastTimePlaySfx + SOUND_DELAY > Time.time) return;
            _lastTimePlaySfx = Time.time;
            _dialogSource.PlayOneShot(audioClip);
        }

        public bool CanPlayAlarm()
        {
            return _lastTimePlayAlarm + SOUND_DELAY < Time.time;
        }

        public void RegisterAlarmPlaying()
        {
            _lastTimePlayAlarm = Time.time;
        }
    }
}
