using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.SuperWeapons;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "InterfaceSfxData", menuName = "Data/Audio/Interface SFX")]
    public sealed class InterfaceSfxData : Data
    {
        [SerializeField] private AudioClip rejected;
        [SerializeField] private AudioClip orbitalAvailable;
        [SerializeField] private AudioClip orbitalBought;
        [SerializeField] private AudioClip orbitalFiring;
        [SerializeField] private AudioClip ionCharging;
        [SerializeField] private AudioClip ionAvailable;
        [SerializeField] private AudioClip ionFiring;
        [SerializeField] private AudioClip hypervelocityCharging;
        [SerializeField] private AudioClip hypervelocityAvailable;
        [SerializeField] private AudioClip hypervelocityFiring;
        [SerializeField] private AudioClip abilityActivated;
        [SerializeField] private AudioClip abilityCanceled;
        [SerializeField] private DictionaryWrapper<FactionType, FactionAnnouncerSet> factionAnnouncers;
        [SerializeField] private AudioClip empireSelected;
        [SerializeField] private AudioClip rebellionSelected;

        public AudioClip Rejected => rejected;
        public AudioClip GetOrbitalAvailable(SuperWeaponType type) => type switch
        {
            SuperWeaponType.IonCannon => ionAvailable,
            SuperWeaponType.HypervelocityGun => hypervelocityAvailable,
            SuperWeaponType.PlasmaCannon => orbitalAvailable,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
        public AudioClip GetOrbitalBought(SuperWeaponType type) => type switch
        {
            SuperWeaponType.IonCannon => ionCharging,
            SuperWeaponType.HypervelocityGun => hypervelocityCharging,
            SuperWeaponType.PlasmaCannon => orbitalBought,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
        public AudioClip GetOrbitalFiring(SuperWeaponType type) => type switch
        {
            SuperWeaponType.IonCannon => ionFiring,
            SuperWeaponType.HypervelocityGun => hypervelocityFiring,
            SuperWeaponType.PlasmaCannon => orbitalFiring,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
        public AudioClip AbilityActivated => abilityActivated;
        public AudioClip AbilityCanceled => abilityCanceled;
        public AudioClip EmpireSelected => empireSelected;
        public AudioClip RebellionSelected => rebellionSelected;
        public FactionAnnouncerSet GetAnnouncer(FactionType faction) => factionAnnouncers.Dictionary[faction];
    }
}
