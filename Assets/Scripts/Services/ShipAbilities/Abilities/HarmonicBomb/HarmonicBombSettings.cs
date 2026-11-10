using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class HarmonicBombSettings : ShipAbilitySettings, IShipAbilityAutoUseRule
    {
        [SerializeField] private float radius = 70f;
        [SerializeField] private int minimumTargets = 3;
        [SerializeField] private HarmonicBombView viewPrefab;

        public float Radius => radius;
        public HarmonicBombView ViewPrefab => viewPrefab;

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            dependencies.HarmonicBombs.HasEnoughTargets(caster.Entity.Owner, caster.WorldPosition, radius, minimumTargets);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new HarmonicBombAbility(dependencies.HarmonicBombs, this);
    }
}
