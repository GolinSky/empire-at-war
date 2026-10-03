using System;
using EmpireAtWar.Utils.Random;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class SfxProfile
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private RandomAudioClips clips;

        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField] private float priority = 50f;
        [SerializeField] private float cooldown;

        [SerializeField] private int maxInstances = 4;

        [SerializeField] private bool loop;

        public float Volume => volume;
        public float Priority => priority;
        public int MaxInstances => maxInstances;
        public bool Loop => loop;
        public float Cooldown => cooldown;

        public AudioClip GetClip() => clip != null ? clip : clips.GetRandom();
    }
}
