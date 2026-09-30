using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
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
        [SerializeField] private WeaponAudioProfile[] weapons;
        [SerializeField] private ShipAbilityAudioProfile[] abilities;
        [SerializeField] private SfxProfile engine;
        [SerializeField] private SfxProfile acceleration;
        [SerializeField] private SfxProfile alarm;
        [SerializeField] private SfxProfile hyperspace;
        [SerializeField] private DictionaryWrapper<FactionType, ShipVoiceSet> voices;
        [SerializeField] private FloatRange alarmDelay;
        [SerializeField] private int poolVoices = 20;
        [SerializeField] private int perShipVoices = 2;
        [SerializeField] private int engineVoices = 4;
        [SerializeField] private int startsPerWindow = 3;
        [SerializeField] private float startWindow = 0.06f;
        [SerializeField] private float voiceCooldown = 2f;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.5f;
        [SerializeField] private float edgeGain = 0.45f;
        [SerializeField] private float offscreenMargin = 0.15f;
        [SerializeField] private float minZoomGain = 0.6f;
        [SerializeField] private float voiceDuckGain = 0.4f;

        public SfxProfile Engine => engine;
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

        public SfxProfile GetWeapon(WeaponType type)
        {
            foreach (WeaponAudioProfile profile in weapons)
                if (profile.WeaponType == type) return profile.Sfx;
            throw new System.InvalidOperationException($"Missing weapon audio for {type}.");
        }

        public ShipAbilityAudioProfile GetAbility(ShipAbilityId id)
        {
            foreach (ShipAbilityAudioProfile profile in abilities)
                if (profile.AbilityId == id) return profile;
            throw new System.InvalidOperationException($"Missing ship SFX for {id}.");
        }
    }
}
