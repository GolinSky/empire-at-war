using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Components.Ship.Audio
{
    [CreateAssetMenu(fileName = "AudioShipDialogData", menuName = "Data/Audio/AudioShipDialogData")]
    public class AudioShipDialogData : Data
    {
        public enum ClipType
        {
            Dialog,
            Move,
            Attack,
            AlarmSights,
            Damage
        }

        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> dialogAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> moveAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> attackAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> alarmSightsAudioClipsWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, List<AudioClip>> damageAudioClipsWrapper;

        public int GetClipCount(FactionType factionType, ClipType clipType)
        {
            return GetClips(factionType, clipType).Count;
        }

        public AudioClip GetClip((FactionType FactionType, ClipType ClipType, int Index) request)
        {
            return GetClips(request.FactionType, request.ClipType)[request.Index];
        }

        private List<AudioClip> GetClips(FactionType factionType, ClipType clipType)
        {
            return clipType switch
            {
                ClipType.Dialog => dialogAudioClipsWrapper.Dictionary[factionType],
                ClipType.Move => moveAudioClipsWrapper.Dictionary[factionType],
                ClipType.Attack => attackAudioClipsWrapper.Dictionary[factionType],
                ClipType.AlarmSights => alarmSightsAudioClipsWrapper.Dictionary[factionType],
                ClipType.Damage => damageAudioClipsWrapper.Dictionary[factionType],
                _ => throw new System.ArgumentOutOfRangeException(nameof(clipType), clipType, null)
            };
        }
    }
}
