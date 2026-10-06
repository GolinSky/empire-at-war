using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FullSalvoSettings : ShipAbilitySettings
    {
        [SerializeField] private float projectileFireDelayMultiplier = 0.33f;
        [SerializeField] private float otherFireDelayMultiplier = 3f;

        public float ProjectileFireDelayMultiplier => projectileFireDelayMultiplier;
        public float OtherFireDelayMultiplier => otherFireDelayMultiplier;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<FullSalvoAbility>(new object[] { this });
    }
}
