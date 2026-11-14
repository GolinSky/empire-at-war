using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class TorpedoSalvoSettings : ShipAbilitySettings
    {
        [SerializeField] private float fireDelayMultiplier = 0.5f;

        public float FireDelayMultiplier => fireDelayMultiplier;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new TorpedoSalvoAbility(this);
    }
}
