using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Audio
{
    
    [CreateAssetMenu(fileName = "MusicAudioModel", menuName = "Model/Audio/Music")]
    public class MusicAudioModel:Model
    {
        [SerializeField] private DictionaryWrapper<SceneType, MusicData> musicWrapper;

        public List<AudioClip> GetMusicList(SceneType sceneType, FactionType factionType)
        {
            return musicWrapper.Dictionary[sceneType].Dictionary[factionType];// refactor this
        }
    }


    [Serializable]
    public class MusicData:DictionaryWrapper<FactionType, List<AudioClip>>
    {
        
    }
}