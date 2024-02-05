using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.SceneService;
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
    public class AudioService: Service, IInitializable, ILateDisposable
    {
        private readonly ISceneService sceneService;
        private readonly MusicAudioModel musicAudioModel;
        private readonly AudioSource audioSource;
        private readonly FactionType factionType;
        private readonly Random random;

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
            List<AudioClip> clips = musicAudioModel.GetMusicList(sceneType, factionType);
            int randomIndex = random.Next(clips.Count);
            audioSource.clip = clips.ElementAt(randomIndex);
            audioSource.loop = true;//temp solution
            audioSource.Play();
        }
    }
}