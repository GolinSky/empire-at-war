using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.EditorSerialization;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.Models.Audio
{
    public interface IAudioShipDialogModelObserver : IModelObserver
    {

    }

    [CreateAssetMenu(fileName = "AudioShipDialogModel", menuName = "Model/Audio/AudioShipDialogModel")]
    public class AudioShipDialogModel : Model, IAudioShipDialogModelObserver
    {
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> dialogAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> moveAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> attackAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> alarmSightsAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> damageAudioClipsWrapper;
        private AudioClip hyperSpaceAudioClip;
        private Random dialogRandom = new Random();
        private Random attackRandom = new Random();
        private Random moveRandom = new Random();
        private Random alarmSightsRandom = new Random();
        private Random damaageRandom = new Random();
        
        [Inject(Id = PlayerType.Player)] 
        private FactionType PlayerFactionType { get; }
        
        [Inject(Id = PlayerType.Opponent)] 
        private FactionType EnemyFactionType { get; }
        
        public AudioClip GetDialogClip(PlayerType playerType)
        {
            return GetClip(playerType, dialogAudioClipsWrapper.Dictionary, dialogRandom);
        }
        
        public AudioClip GetAttackClip(PlayerType playerType)
        {
            return GetClip(playerType, attackAudioClipsWrapper.Dictionary, attackRandom);
        }
        
        public AudioClip GetMoveClip(PlayerType playerType)
        {
            return GetClip(playerType, moveAudioClipsWrapper.Dictionary, moveRandom);
        }
        
        public AudioClip GetAlarmSightsClip(PlayerType playerType)
        {
            return GetClip(playerType, alarmSightsAudioClipsWrapper.Dictionary, alarmSightsRandom);
        }

        public AudioClip GetDamageClip(PlayerType playerType)
        {
            return GetClip(playerType, damageAudioClipsWrapper.Dictionary, damaageRandom);
        }
        private AudioClip GetClip(PlayerType playerType, Dictionary<FactionType, List<AudioClip>> clipDictionary, Random random)
        {
            FactionType factionType = playerType == PlayerType.Player ? PlayerFactionType : EnemyFactionType;
            List<AudioClip> clips = clipDictionary[factionType];
            return clips[random.Next(clips.Count)];
        }
    }
}