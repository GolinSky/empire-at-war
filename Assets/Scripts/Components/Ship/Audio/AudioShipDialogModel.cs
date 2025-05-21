using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.Components.Ship.Audio
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
        private AudioClip _hyperSpaceAudioClip;
        private Random _dialogRandom = new Random();
        private Random _attackRandom = new Random();
        private Random _moveRandom = new Random();
        private Random _alarmSightsRandom = new Random();
        private Random _damaageRandom = new Random();
        
        [Inject(Id = PlayerType.Player)] 
        private FactionType PlayerFactionType { get; }
        
        [Inject(Id = PlayerType.Opponent)] 
        private FactionType EnemyFactionType { get; }
        
        public AudioClip GetDialogClip(PlayerType playerType)
        {
            return GetClip(playerType, dialogAudioClipsWrapper.Dictionary, _dialogRandom);
        }
        
        public AudioClip GetAttackClip(PlayerType playerType)
        {
            return GetClip(playerType, attackAudioClipsWrapper.Dictionary, _attackRandom);
        }
        
        public AudioClip GetMoveClip(PlayerType playerType)
        {
            return GetClip(playerType, moveAudioClipsWrapper.Dictionary, _moveRandom);
        }
        
        public AudioClip GetAlarmSightsClip(PlayerType playerType)
        {
            return GetClip(playerType, alarmSightsAudioClipsWrapper.Dictionary, _alarmSightsRandom);
        }

        public AudioClip GetDamageClip(PlayerType playerType)
        {
            return GetClip(playerType, damageAudioClipsWrapper.Dictionary, _damaageRandom);
        }
        private AudioClip GetClip(PlayerType playerType, Dictionary<FactionType, List<AudioClip>> clipDictionary, Random random)
        {
            FactionType factionType = playerType == PlayerType.Player ? PlayerFactionType : EnemyFactionType;
            List<AudioClip> clips = clipDictionary[factionType];
            return clips[random.Next(clips.Count)];
        }
    }
}