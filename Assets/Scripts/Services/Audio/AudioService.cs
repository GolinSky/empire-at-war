using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.SceneService;
using ModestTree;
using UnityEngine;
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
        private readonly MusicAudioModel musicAudioModel;
        private readonly AudioSource audioSource;
        private readonly FactionType factionType;
        private readonly Random random;
        private List<AudioClip> clips;
        private float duration;
        private bool isMusicPlaying;
        public AudioService(ISceneService sceneService, IRepository repository, SkirmishGameData skirmishGameData)
        {
            this.sceneService = sceneService;
            musicAudioModel = repository.Load<MusicAudioModel>(nameof(MusicAudioModel));
            factionType = skirmishGameData.PlayerFactionType;
            audioSource = Object.Instantiate(repository.LoadComponent<AudioSource>("MusicSource"));
            Object.DontDestroyOnLoad(audioSource);
            random = new Random();
        }
        
        public void Initialize()
        {
            OnSceneLoad(sceneService.LoadingScene);
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
            clips = musicAudioModel.GetMusicList(sceneType, factionType);
            PlayMusicInternal();
        }

        private void PlayMusicInternal()
        {
            if(clips == null || clips.Count == 0) return;
            int randomIndex = random.Next(clips.Count);
            AudioClip audioClip = clips.ElementAt(randomIndex);
            audioSource.clip = audioClip;
            audioSource.Play();
            duration = audioClip.length + Time.time;
            isMusicPlaying = true;
        }

        public void Tick()
        {
            if(!isMusicPlaying) return;

            if (Time.time > duration)
            {
                isMusicPlaying = false;
                PlayMusicInternal();
            }
        }
    }
}