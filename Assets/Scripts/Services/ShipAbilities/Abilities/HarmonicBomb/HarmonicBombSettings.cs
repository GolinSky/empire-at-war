using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class HarmonicBombSettings : ShipAbilitySettings
    {
        [SerializeField] private float radius = 35f;
        [SerializeField] private float centerDamage = 120f;
        [SerializeField] private int minimumTargets = 3;
        [SerializeField] private HarmonicBombView viewPrefab;

        public float Radius => radius;
        public float CenterDamage => centerDamage;
        public HarmonicBombView ViewPrefab => viewPrefab;

        public bool ShouldAutoDrop(HarmonicBombService bombs, IShipAbilityFacade caster) =>
            bombs.HasEnoughTargets(caster.Entity.Owner, caster.WorldPosition, radius, minimumTargets);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new HarmonicBombAbility(dependencies.HarmonicBombs, this);
    }
}
