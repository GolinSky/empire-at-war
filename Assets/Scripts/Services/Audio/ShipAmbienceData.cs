using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "ShipAmbienceData", menuName = "Data/Audio/Ship Ambience")]
    public sealed class ShipAmbienceData : Data
    {
        [SerializeField] private AudioClip[] loops;
        [SerializeField, Min(0f)] private float innerRadius = 50f;
        [SerializeField, Min(1f)] private float outerRadius = 300f;
        [SerializeField, Min(0.01f)] private float fadeTime = 1f;
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
        [SerializeField, Min(0.01f)] private float updateInterval = 0.25f;

        public float InnerRadius => innerRadius;
        public float OuterRadius => outerRadius;
        public float FadeTime => fadeTime;
        public float Volume => volume;
        public float UpdateInterval => updateInterval;
        public AudioClip GetClip() => loops[Random.Range(0, loops.Length)];
    }
}
