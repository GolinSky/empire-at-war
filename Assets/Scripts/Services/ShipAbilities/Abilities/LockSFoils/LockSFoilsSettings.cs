using System;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Automatic use: the wings close while the unit travels and reopen once it stops travelling.</summary>
    [Serializable]
    public sealed class LockSFoilsSettings : ShipAbilitySettings, IShipAbilityAutoUseRule, IShipAbilityAutoStopRule
    {
        [SerializeField] private CombatStatModifier statModifier;
        public CombatStatModifier StatModifier => statModifier;

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            IsTravelling(caster);

        public bool ShouldAutoStop(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            !IsTravelling(caster);

        private static bool IsTravelling(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IUnitOrderObserverFacade>().CurrentOrder is
                UnitOrderType.Move or UnitOrderType.WaypointMove or UnitOrderType.Retreat;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new LockSFoilsAbility(this);
    }
}
