using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class TractorBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private float speedMultiplier = 0.25f;
        [SerializeField] private ShipClass[] targetClasses = { ShipClass.Corvette, ShipClass.Frigate };
        [SerializeField] private WeaponProfile beam;

        public float SpeedMultiplier => speedMultiplier;
        public WeaponProfile Beam => beam;

        public bool CanTarget(ShipClass shipClass) => Array.IndexOf(targetClasses, shipClass) >= 0;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<TractorBeamAbility>(new object[] { this });
    }
}
