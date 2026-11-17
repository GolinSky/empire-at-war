using System;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class GravityWellSettings : ShipAbilitySettings
    {
        [SerializeField] private GravityWellView viewPrefab;

        public GravityWellView ViewPrefab => viewPrefab;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new GravityWellAbility(dependencies.BattleAreas, dependencies.GravityWells, this);
    }
}
