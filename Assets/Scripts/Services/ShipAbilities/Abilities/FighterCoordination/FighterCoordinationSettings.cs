using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterCoordinationSettings : ShipAbilitySettings
    {
        [SerializeField] private float commandRadius = 750f;

        public float CommandRadius => commandRadius;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<FighterCoordinationAbility>(new object[] { this });
    }
}
