using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FullSalvoSettings : ShipAbilitySettings
    {
        [SerializeField] private float projectileFireDelayMultiplier = 0.33f;
        [SerializeField] private float otherFireDelayMultiplier = 3f;

        public float ProjectileFireDelayMultiplier => projectileFireDelayMultiplier;
        public float OtherFireDelayMultiplier => otherFireDelayMultiplier;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new FullSalvoAbility(this);
    }
}
