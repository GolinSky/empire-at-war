using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterCoordinationSettings : ShipAbilitySettings
    {
        [SerializeField] private float commandRadius = 750f;

        public float CommandRadius => commandRadius;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new FighterCoordinationAbility(dependencies.Entities, this, dependencies.Relations);
    }
}
