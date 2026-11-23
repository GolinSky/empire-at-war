using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ProtonBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private DamageType damageType = DamageType.Beam;

        [SerializeField] private float damage;

        [SerializeField] private Color beamColor = new Color(0.04f, 0.32f, 1f, 1f);

        public float Damage => damage;
        public DamageType DamageType => damageType;
        public Color BeamColor => beamColor;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new ProtonBeamAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData,
                dependencies.Vision, dependencies.Session);
    }
}
