using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterCoordinationSettings : ShipAbilitySettings
    {
        [SerializeField] private float commandRadius = 750f;

        public float CommandRadius => commandRadius;

        internal override HardPointType RequiredSubsystem => HardPointType.CommandArray;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new FighterCoordinationAbility(dependencies.Entities, this, dependencies.Relations);
    }
}
