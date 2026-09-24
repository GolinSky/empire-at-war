using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ProtonBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private float damage;
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private ProtonBeamView viewPrefab;

        public float Damage => damage;
        public WeaponType WeaponType => weaponType;
        public ProtonBeamView ViewPrefab => viewPrefab;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<ProtonBeamAbility>(new object[] { this });
    }
}
