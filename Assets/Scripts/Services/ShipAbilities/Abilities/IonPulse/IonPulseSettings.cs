using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class IonPulseSettings : ShipAbilitySettings, IShipAbilityTargetRule, IShipAbilityReadiness
    {
        [SerializeField] private IonPulseView viewPrefab;
        [SerializeField] private float alignmentTolerance = 5f;
        [SerializeField] private float waveSpeed = 35f;
        [SerializeField] private float waveRadius = 60f;
        [SerializeField] private float waveThickness = 8f;
        [SerializeField] private float disableDuration = 10f;

        public IonPulseView ViewPrefab => viewPrefab;
        public float AlignmentTolerance => alignmentTolerance;
        public float WaveSpeed => waveSpeed;
        public float WaveRadius => waveRadius;
        public float WaveThickness => waveThickness;
        public float DisableDuration => disableDuration;

        public override float GetEffectDuration(float activeDuration) => disableDuration;

        // The hull cannot turn during the hyperspace arrival, so the pulse waits until the ship is in the battle.
        public bool IsReady(IShipAbilityFacade caster) =>
            !(caster.Entity.TryGetFacade(out IShipArrivalFacade arrival) && arrival.IsArriving);

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) => !target.HealthModel.ShipClass.IsStrikecraft();

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new IonPulseAbility(this, dependencies.Entities, dependencies.Relations);
    }
}
