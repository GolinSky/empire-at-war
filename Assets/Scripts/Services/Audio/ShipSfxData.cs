using UnityEngine;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "ShipSfxData", menuName = "Data/Audio/Ship SFX")]
    public sealed class ShipSfxData : ScriptableObject
    {
        [SerializeField] private ShipSfxView viewPrefab;
        [SerializeField] private ShipAbilityAudioProfile[] abilities;
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip accelerationClip;
        [SerializeField, Range(0f, 1f)] private float engineVolume = 0.12f;
        [SerializeField, Range(0f, 1f)] private float accelerationVolume = 0.35f;
        [SerializeField] private float minDistance = 15f;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float zoomReferenceHeight = 180f;

        public ShipSfxView ViewPrefab => viewPrefab;
        public AudioClip EngineLoop => engineLoop;
        public AudioClip AccelerationClip => accelerationClip;
        public float EngineVolume => engineVolume;
        public float AccelerationVolume => accelerationVolume;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float ZoomReferenceHeight => zoomReferenceHeight;

        public ShipAbilityAudioProfile GetAbility(ShipAbilityId id)
        {
            foreach (ShipAbilityAudioProfile profile in abilities)
                if (profile.AbilityId == id) return profile;
            throw new System.InvalidOperationException($"Missing ship SFX for {id}.");
        }
    }
}
