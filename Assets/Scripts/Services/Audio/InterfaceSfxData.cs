using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "InterfaceSfxData", menuName = "Data/Audio/Interface SFX")]
    public sealed class InterfaceSfxData : Data
    {
        [SerializeField] private AudioClip rejected;
        [SerializeField] private AudioClip orbitalAvailable;
        [SerializeField] private AudioClip orbitalBought;
        [SerializeField] private AudioClip orbitalFiring;
        [SerializeField] private AudioClip abilityActivated;
        [SerializeField] private AudioClip abilityCanceled;

        public AudioClip Rejected => rejected;
        public AudioClip OrbitalAvailable => orbitalAvailable;
        public AudioClip OrbitalBought => orbitalBought;
        public AudioClip OrbitalFiring => orbitalFiring;
        public AudioClip AbilityActivated => abilityActivated;
        public AudioClip AbilityCanceled => abilityCanceled;
    }
}
