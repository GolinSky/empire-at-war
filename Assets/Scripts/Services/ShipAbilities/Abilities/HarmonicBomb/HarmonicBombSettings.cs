using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class HarmonicBombSettings : ShipAbilitySettings, IShipAbilityAutoUseRule
    {
        [SerializeField] private float radius = 210f;
        [SerializeField] private float damage = 360f;
        [SerializeField] private float fighterDamageMultiplier = 2f;
        [SerializeField] private DamageType damageType = DamageType.HypervelocityGun;
        [SerializeField] private int minimumTargets = 3;
        [SerializeField] private HarmonicBombView viewPrefab;

        public float Radius => radius;
        public float Damage => damage;
        public float FighterDamageMultiplier => fighterDamageMultiplier;
        public DamageType DamageType => damageType;
        public HarmonicBombView ViewPrefab => viewPrefab;

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            dependencies.HarmonicBombs.HasEnoughTargets(caster.Entity.Owner, caster.WorldPosition, radius, minimumTargets);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new HarmonicBombAbility(dependencies.HarmonicBombs, this);
    }
}
