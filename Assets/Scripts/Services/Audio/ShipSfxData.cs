using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "ShipSfxData", menuName = "Data/Audio/Ship SFX")]
    public sealed class ShipSfxData : Data
    {
        [Tooltip("Default sound of each weapon family.")]
        [SerializeField] private WeaponFamilyAudioProfile[] weaponFamilies;
        [Tooltip("Weapons that sound different from their family, plus weapons outside the families.")]
        [SerializeField] private WeaponAudioProfile[] weapons;
        [SerializeField] private ShipAbilityAudioProfile[] abilities;
        [SerializeField] private SfxProfile fallbackEngine;
        [SerializeField] private SfxProfile fallbackExplosion;
        [SerializeField] private DictionaryWrapper<ShipType, ShipUnitAudioProfile> ships;
        [SerializeField] private DictionaryWrapper<SquadronType, ShipUnitAudioProfile> squadrons;
        [SerializeField] private SfxProfile abilityActivated;
        [SerializeField] private SfxProfile abilityCanceled;
        [SerializeField] private SfxProfile shieldsDown;
        [SerializeField] private SfxProfile shieldsUp;
        [SerializeField] private SfxProfile shieldImpact;
        [SerializeField] private float shieldUpThreshold = 0.1f;
        [SerializeField] private float shieldCueCooldown = 4f;
        [SerializeField] private SfxProfile acceleration;
        [SerializeField] private SfxProfile alarm;
        [SerializeField] private SfxProfile hyperspace;
        [SerializeField] private DictionaryWrapper<FactionType, ShipVoiceSet> voices;
        [SerializeField] private FloatRange alarmDelay;

        [SerializeField] private float startWindow = 0.06f;
        [SerializeField] private float voiceCooldown = 2f;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.5f;
        [SerializeField] private float edgeGain = 0.45f;
        [SerializeField] private float offscreenMargin = 0.15f;
        [SerializeField] private float minZoomGain = 0.6f;
        [SerializeField] private float voiceDuckGain = 0.4f;

        [SerializeField] private int poolVoices = 20;
        [SerializeField] private int perShipVoices = 2;
        [SerializeField] private int engineVoices = 6;
        [SerializeField] private int startsPerWindow = 3;

        public SfxProfile AbilityActivated => abilityActivated;
        public SfxProfile AbilityCanceled => abilityCanceled;
        public SfxProfile ShieldsDown => shieldsDown;
        public SfxProfile ShieldsUp => shieldsUp;
        public SfxProfile ShieldImpact => shieldImpact;
        public float ShieldUpThreshold => shieldUpThreshold;
        public float ShieldCueCooldown => shieldCueCooldown;
        public SfxProfile Acceleration => acceleration;
        public SfxProfile Alarm => alarm;
        public SfxProfile Hyperspace => hyperspace;
        public float AlarmDelay => UnityEngine.Random.Range(alarmDelay.Min, alarmDelay.Max);
        public int PoolVoices => poolVoices;
        public int PerShipVoices => perShipVoices;
        public int EngineVoices => engineVoices;
        public int StartsPerWindow => startsPerWindow;
        public float StartWindow => startWindow;
        public float VoiceCooldown => voiceCooldown;
        public float VoiceVolume => voiceVolume;
        public float EdgeGain => edgeGain;
        public float OffscreenMargin => offscreenMargin;
        public float MinZoomGain => minZoomGain;
        public float VoiceDuckGain => voiceDuckGain;

        public ShipVoiceSet GetVoiceSet(FactionType faction) => voices.Dictionary[faction];

        public ShipUnitAudioProfile GetUnit(ShipType type) => ships.Dictionary.TryGetValue(type, out var profile)
            ? profile : new ShipUnitAudioProfile(fallbackEngine, fallbackExplosion, alarm);

        public ShipUnitAudioProfile GetUnit(SquadronType type) => squadrons.Dictionary.TryGetValue(type, out var profile)
            ? profile : new ShipUnitAudioProfile(fallbackEngine, fallbackExplosion, alarm);

        /// <summary>The weapon's own sound if it has one, otherwise its family's sound.</summary>
        public SfxProfile GetWeapon(WeaponType type, WeaponFamily family)
        {
            if (TryGetWeaponOverride(type, out SfxProfile sfx)) return sfx;
            foreach (WeaponFamilyAudioProfile profile in weaponFamilies)
                if (profile.Family == family) return profile.Sfx;
            throw new System.InvalidOperationException($"Missing weapon audio for {type} and its family {family}.");
        }

        /// <summary>Sound of a weapon outside the weapon families, such as a super weapon.</summary>
        public SfxProfile GetWeapon(WeaponType type)
        {
            if (TryGetWeaponOverride(type, out SfxProfile sfx)) return sfx;
            throw new System.InvalidOperationException($"Missing weapon audio for {type}.");
        }

        private bool TryGetWeaponOverride(WeaponType type, out SfxProfile sfx)
        {
            foreach (WeaponAudioProfile profile in weapons)
            {
                if (profile.WeaponType != type) continue;
                sfx = profile.Sfx;
                return true;
            }

            sfx = null;
            return false;
        }

        public ShipAbilityAudioProfile GetAbility(ShipAbilityId id)
        {
            foreach (ShipAbilityAudioProfile profile in abilities)
                if (profile.AbilityId == id) return profile;
            throw new System.InvalidOperationException($"Missing ship SFX for {id}.");
        }
    }
}
