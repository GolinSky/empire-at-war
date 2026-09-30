using System;
using EmpireAtWar.Utils.Random;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class ShipVoiceSet
    {
        [SerializeField] private RandomAudioClips selection;
        [SerializeField] private RandomAudioClips move;
        [SerializeField] private RandomAudioClips attack;
        [SerializeField] private RandomAudioClips alarm;
        [SerializeField] private RandomAudioClips damage;

        public AudioClip Selection => selection.GetRandom();
        public AudioClip Move => move.GetRandom();
        public AudioClip Attack => attack.GetRandom();
        public AudioClip Alarm => alarm.GetRandom();
        public AudioClip Damage => damage.GetRandom();
    }
}
