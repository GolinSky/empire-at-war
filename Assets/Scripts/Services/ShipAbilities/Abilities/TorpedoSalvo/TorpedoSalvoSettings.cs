using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class TorpedoSalvoSettings : ShipAbilitySettings
    {
        [SerializeField] private float fireDelayMultiplier = 2f / 3f;

        public float FireDelayMultiplier => fireDelayMultiplier;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<TorpedoSalvoAbility>(new object[] { this });
    }
}
