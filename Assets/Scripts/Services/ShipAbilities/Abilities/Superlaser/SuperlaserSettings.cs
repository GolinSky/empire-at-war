using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Spinal superlaser: one sustained beam from the bound muzzle against frigates and larger, stations and structures.</summary>
    [Serializable]
    public sealed class SuperlaserSettings : ShipAbilitySettings, IShipAbilityTargetRule, IShipAbilityTargetPriority
    {
        [SerializeField] private DamageType damageType = DamageType.Superlaser;
        [SerializeField] private float damage;
        [SerializeField] private Color beamColor = new Color(1f, 0.12f, 0.08f, 1f);

        [Header("Damage scale by target")]
        [SerializeField] private float vsStation = 4f;
        [SerializeField] private float vsStructure = 2f;
        [SerializeField] private float vsDreadnought = 1.5f;
        [SerializeField] private float vsBattleCruiser = 1f;
        [SerializeField] private float vsCapital = 0.8f;
        [SerializeField] private float vsHeavyCruiser = 0.6f;
        [SerializeField] private float vsCruiser = 0.5f;
        [SerializeField] private float vsOther = 0.2f;

        public DamageType DamageType => damageType;
        public float Damage => damage;
        public Color BeamColor => beamColor;
        public float VsStation => vsStation;
        public float VsStructure => vsStructure;
        public float VsDreadnought => vsDreadnought;
        public float VsBattleCruiser => vsBattleCruiser;
        public float VsCapital => vsCapital;
        public float VsHeavyCruiser => vsHeavyCruiser;
        public float VsCruiser => vsCruiser;
        public float VsOther => vsOther;

        /// <summary>Scale on the whole beam (shields and hull) against this target.</summary>
        public float GetDamageMultiplier(IEntity target)
        {
            if (target.IsPlayerBase()) return vsStation;
            return target.HealthModel.ShipClass switch
            {
                ShipClass.Structure => vsStructure,
                ShipClass.Dreadnought => vsDreadnought,
                ShipClass.BattleCruiser => vsBattleCruiser,
                ShipClass.Capital => vsCapital,
                ShipClass.HeavyCruiser => vsHeavyCruiser,
                ShipClass.Cruiser => vsCruiser,
                _ => vsOther
            };
        }

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) =>
            target.HealthModel.ShipClass is ShipClass.Frigate or ShipClass.Cruiser or ShipClass.HeavyCruiser or
                ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought or ShipClass.Structure ||
            target.IsPlayerBase();

        // Automatic order: Capital > BattleCruiser > Dreadnought > station or structure, then any other valid ship.
        public int GetTargetPriority(IEntity target)
        {
            if (target.IsPlayerBase() || target.HealthModel.ShipClass == ShipClass.Structure) return 1;
            return target.HealthModel.ShipClass switch
            {
                ShipClass.Capital => 4,
                ShipClass.BattleCruiser => 3,
                ShipClass.Dreadnought => 2,
                _ => 0
            };
        }

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new SuperlaserAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData,
                dependencies.Vision, dependencies.Session);
    }
}
