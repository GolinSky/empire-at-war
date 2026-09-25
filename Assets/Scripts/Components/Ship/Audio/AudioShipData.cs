using EmpireAtWar.Utils.Random;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Audio;
using UnityEngine.AddressableAssets;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    [CreateAssetMenu(fileName = "AudioShipData", menuName = "Data/Audio/AudioShipData")]
    public class AudioShipData : Data, ILateDisposable
    {
        [SerializeField] private AssetReferenceT<AudioClip> hyperSpaceAudioReference;
        [SerializeField] private RandomAudioClips alarmRandomClips;
        [SerializeField] private RandomAudioClips backgroundClips;
        [SerializeField] private ShipSfxData shipSfx;
        [Header("Weapon fire")]
        [SerializeField] private WeaponAudioView weaponAudioPrefab;
        [SerializeField] private WeaponAudioProfile[] weaponSounds;
        [SerializeField] private float weaponMinDistance = 20f;
        [SerializeField] private float weaponMaxDistance = 120f;
        [SerializeField] private float weaponZoomReferenceHeight = 180f;
        private AudioClip _hyperSpaceAudioClip;

        public WeaponAudioView WeaponAudioPrefab => weaponAudioPrefab;
        public ShipSfxData ShipSfx => shipSfx;
        public float WeaponMinDistance => weaponMinDistance;
        public float WeaponMaxDistance => weaponMaxDistance;
        public float WeaponZoomReferenceHeight => weaponZoomReferenceHeight;

        public WeaponAudioProfile GetWeaponSound(WeaponType weaponType)
        {
            foreach (WeaponAudioProfile sound in weaponSounds)
                if (sound.WeaponType == weaponType) return sound;

            throw new System.InvalidOperationException($"Missing weapon audio for {weaponType}.");
        }

        [field:SerializeField] public RandomFloat AlarmDelay { get; private set; }

        public AudioClip GetAmbientClip()
        {
            return backgroundClips.GetRandom();
        }

        public AudioClip GetAlarmClip()
        {
            return alarmRandomClips.GetRandom();
        }

        public AudioClip GetHyperSpaceClip()
        {
            if (_hyperSpaceAudioClip == null)
            {
                _hyperSpaceAudioClip = hyperSpaceAudioReference.LoadAssetAsync().WaitForCompletion();
            }

            return _hyperSpaceAudioClip;
        }

        public void LateDispose()
        {
            hyperSpaceAudioReference.ReleaseAsset();
        }
    }
}
