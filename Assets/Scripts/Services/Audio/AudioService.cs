using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.Repository;
using WorkShop.LightWeightFramework.Service;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.Services.Audio
{
    public interface IAudioService:IService
    {
        
    }
    public class AudioService: Service, IInitializable, ILateDisposable, ITickable
    {
        private readonly ISceneService sceneService;
        private readonly IGameModelObserver gameModelObserver;
        private readonly MusicAudioModel musicAudioModel;
        private readonly AudioSource audioSource;
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
            audioSource = Object.Instantiate(repository.LoadComponent<AudioSource>("MusicSource"));
            Object.DontDestroyOnLoad(audioSource);
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
            audioSource.clip = audioClip;
            audioSource.Play();
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
    }
}