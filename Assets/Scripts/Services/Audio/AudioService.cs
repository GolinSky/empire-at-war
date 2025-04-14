using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Components.Service;
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
        private const string SOURCE_PATH = "MusicSource";
        private const string DIALOG_SOURCE_PATH = "AudioDialogSource";
        private readonly ISceneService _sceneService;
        private readonly IGameModelObserver _gameModelObserver;
        private readonly MusicAudioModel _musicAudioModel;
        private readonly AudioSource _backgroundSource;
        private readonly AudioSource _dialogSource;
        private readonly Random _random;
        private readonly ITimer _timer;
        private List<AudioClip> _clips;
        private bool _isMusicPlaying;
        private float _lastTimePlayAlarm;
        private float _lastTimePlaySfx;
        
       
        public AudioService(ISceneService sceneService, IRepository repository, IGameModelObserver gameModelObserver)
        {
            _sceneService = sceneService;
            _gameModelObserver = gameModelObserver;
            _timer = TimerFactory.ConstructTimer();
            _musicAudioModel = repository.Load<MusicAudioModel>(nameof(MusicAudioModel));
            _backgroundSource = Object.Instantiate(repository.LoadComponent<AudioSource>(SOURCE_PATH));
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
            PlayMusicInternal();
        }

        private void PlayMusicInternal()
        {
            if(_clips == null || _clips.Count == 0) return;
            int randomIndex = _random.Next(_clips.Count);
            AudioClip audioClip = _clips.ElementAt(randomIndex);
            _backgroundSource.clip = audioClip;
            _backgroundSource.Play();
            _timer.ChangeDelay(audioClip.length);
            _timer.StartTimer();
            _isMusicPlaying = true;
        }

        public void Tick()
        {
            if(!_isMusicPlaying) return;

            if (_timer.IsComplete)
            {
                _isMusicPlaying = false;
                PlayMusicInternal();
                _timer.StartTimer();
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