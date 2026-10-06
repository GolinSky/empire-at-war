using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class TractorBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private float speedMultiplier = 0.4f;
        [SerializeField] private WeaponProfile beam;

        public float SpeedMultiplier => speedMultiplier;
        public WeaponProfile Beam => beam;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<TractorBeamAbility>(new object[] { this });
    }
}
