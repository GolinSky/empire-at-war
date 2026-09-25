using System;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class ShipAbilityAudioProfile
    {
        [SerializeField] private ShipAbilityId abilityId;
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioClip executionLoop;
        [SerializeField] private AudioClip endClip;
        [SerializeField] private AudioClip restoreClip;
        [SerializeField, Range(0f, 1f)] private float cueVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float executionVolume = 0.12f;

        public ShipAbilityId AbilityId => abilityId;
        public AudioClip StartClip => startClip;
        public AudioClip ExecutionLoop => executionLoop;
        public AudioClip EndClip => endClip;
        public AudioClip RestoreClip => restoreClip;
        public float CueVolume => cueVolume;
        public float ExecutionVolume => executionVolume;
    }
}
