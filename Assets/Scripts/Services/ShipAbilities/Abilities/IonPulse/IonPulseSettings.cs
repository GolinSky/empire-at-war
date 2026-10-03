using System;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class IonPulseSettings : ShipAbilitySettings
    {
        [SerializeField] private IonPulseView viewPrefab;
        [SerializeField] private float chargeDuration = 4f;
        [SerializeField] private float alignmentTimeout = 45f;
        [SerializeField] private float alignmentTolerance = 5f;
        [SerializeField] private float waveSpeed = 35f;
        [SerializeField] private float waveRadius = 60f;
        [SerializeField] private float waveThickness = 8f;
        [SerializeField] private float disableDuration = 12f;
        [SerializeField, Range(0f, 1f)] private float malfunctionChance = 0.1f;

        public IonPulseView ViewPrefab => viewPrefab;
        public float ChargeDuration => chargeDuration;
        public float AlignmentTimeout => alignmentTimeout;
        public float AlignmentTolerance => alignmentTolerance;
        public float WaveSpeed => waveSpeed;
        public float WaveRadius => waveRadius;
        public float WaveThickness => waveThickness;
        public float DisableDuration => disableDuration;
        public float MalfunctionChance => malfunctionChance;

        public override float GetEffectDuration(float activeDuration) => disableDuration;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<IonPulseAbility>(new object[] { this });
    }
}
