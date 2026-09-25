using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class WeaponAudioProfile
    {
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop;
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;

        public WeaponType WeaponType => weaponType;
        public AudioClip Clip => clip;
        public bool Loop => loop;
        public float Volume => volume;
    }
}
