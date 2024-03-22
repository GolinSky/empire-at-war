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
    }

    public class AudioService: Service, IInitializable, ILateDisposable, ITickable, IAudioService
    {
        private const string SourcePath = "MusicSource";
        private const string DialogSourcePath = "AudioDialogSource";
        private readonly ISceneService sceneService;
        private readonly IGameModelObserver gameModelObserver;
        private readonly MusicAudioModel musicAudioModel;
        private readonly AudioSource backgroundSource;
        private readonly AudioSource dialogSource;
        private readonly Random random;
        private readonly ITimer timer;
        private List<AudioClip> clips;
        private bool isMusicPlaying;
        
       
        public AudioService(ISceneService sceneService, IRepository repository, IGameModelObserver gameModelObserver)
        {
            this.sceneService = sceneService;
            this.gameModelObserver = gameModelObserver;
            timer = TimerFactory.ConstructTimer();
            musicAudioModel = repository.Load<MusicAudioModel>(nameof(MusicAudioModel));
            backgroundSource = Object.Instantiate(repository.LoadComponent<AudioSource>(SourcePath));
            dialogSource = Object.Instantiate(repository.LoadComponent<AudioSource>(DialogSourcePath));
            Object.DontDestroyOnLoad(backgroundSource);
            Object.DontDestroyOnLoad(dialogSource);
            random = new Random();
        }
        
        public void Initialize()
        {
            OnSceneLoad(sceneService.TargetScene);
            sceneService.OnSceneActivation += OnSceneLoad;
        }
        
        public void LateDispose()
        {
            sceneService.OnSceneActivation -= OnSceneLoad;
        }
        
        private void OnSceneLoad(SceneType sceneType)
        {
            if(sceneType == SceneType.Loading) return;
            
            PlayMusic(sceneType);
        }

        private void PlayMusic(SceneType sceneType)
        {
            clips = musicAudioModel.GetMusicList(sceneType, gameModelObserver.PlayerFactionType);
            PlayMusicInternal();
        }

        private void PlayMusicInternal()
        {
            if(clips == null || clips.Count == 0) return;
            int randomIndex = random.Next(clips.Count);
            AudioClip audioClip = clips.ElementAt(randomIndex);
            backgroundSource.clip = audioClip;
            backgroundSource.Play();
            timer.ChangeDelay(audioClip.length);
            timer.StartTimer();
            isMusicPlaying = true;
        }

        public void Tick()
        {
            if(!isMusicPlaying) return;

            if (timer.IsComplete)
            {
                isMusicPlaying = false;
                PlayMusicInternal();
                timer.StartTimer();
            }
        }

        public void PlayOneShot(AudioClip audioClip, AudioType audioType)
        {
            dialogSource.PlayOneShot(audioClip);
        }
    }
}