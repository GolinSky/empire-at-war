using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EmpireAtWar.Models.Audio
{
    
    [CreateAssetMenu(fileName = nameof(MusicAudioData), menuName = "Data/Audio/Music")]
    public class MusicAudioData:Data
    {
        [SerializeField] private List<AssetReferenceT<AudioClip>> menuMusicData;
        [SerializeField] private MusicData battleMusicData;

        public List<AssetReferenceT<AudioClip>> GetMusicList(SceneType sceneType, FactionType factionType)
        {
            if (sceneType == SceneType.MainMenu)
            {
                return menuMusicData;
            }

            return battleMusicData.Dictionary[factionType];
        }
    }
}