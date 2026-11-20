using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// The one automatic ability decision for every owner (round 5, E10): ability auto-use rules, then the auto-power
    /// roles from <see cref="AutoPowerCombatSnapshot"/>, confirmed over <see cref="AutoPowerTuning.ConfirmationChecks"/>
    /// checks (an urgent shield boost fires at once). The player's auto-power and the AI both call it; each owns its
    /// own decider, so monitors never mix owners.
    /// </summary>
    public sealed class AutoPowerDecider
    {
        private readonly ShipAbilityService _abilities;
        private readonly AutoPowerCombatSnapshot _combat;
        private readonly ShipAbilityAutoUse _autoUse;
        private readonly AutoPowerTuning _tuning;
        private readonly Func<IShipAbilityFacade, ShipAbilityId, IEntity, bool> _activate;
        private readonly Dictionary<long, AutoPowerMonitor> _monitors = new Dictionary<long, AutoPowerMonitor>();

        /// <param name="activate">Starts the ability: a request from a player, a direct start for the host AI.</param>
        public AutoPowerDecider(ShipAbilityService abilities, AutoPowerCombatSnapshot combat, ShipAbilityAutoUse autoUse,
            AutoPowerTuning tuning, Func<IShipAbilityFacade, ShipAbilityId, IEntity, bool> activate)
        {
            _abilities = abilities;
            _combat = combat;
            _autoUse = autoUse;
            _tuning = tuning;
            _activate = activate;
        }

        public void Capture() => _combat.Capture();

        public void Suspend(long id) => GetMonitor(id).Suspend();

        public void SuspendAll()
        {
            foreach (AutoPowerMonitor monitor in _monitors.Values) monitor.Suspend();
        }

        public void HoldManual(long id, float now) => GetMonitor(id).HoldManual(now);

        public void Remove(long id) => _monitors.Remove(id);

        public void Clear() => _monitors.Clear();

        /// <summary>Samples shield loss only, between checks the owner's pace skips.</summary>
        public void Observe(IEntity entity, IShipAbilityFacade caster, IShipAutoPowerFacade automation, float now)
        {
            if (automation != null)
                GetMonitor(entity.Id).Sample(now, caster.Health.Shields, automation.MaxShields,
                    automation.ShieldSuppressed, automation.ShieldRevision);
        }

        /// <summary>One check of one caster; true when it started an ability.</summary>
        /// <param name="automation">Null for a caster without auto-power roles (auto-use rules only).</param>
        public bool Check(IEntity entity, IShipAbilityFacade caster, IShipAutoPowerFacade automation, float now)
        {
            AutoPowerMonitor monitor = GetMonitor(entity.Id);
            AutoPowerShieldLoss shieldLoss = automation == null ? AutoPowerShieldLoss.None
                : monitor.Sample(now, caster.Health.Shields, automation.MaxShields, automation.ShieldSuppressed,
                    automation.ShieldRevision);
            // An active shield boost is the one ability Weapons may stack on; any other active ability waits.
            bool shieldsBoosted = false;
            foreach (ShipAbilitySlot slot in caster.Slots)
            {
                if (slot.State != ShipAbilityState.Active) continue;
                if (slot.Definition.AutoPower != AutoPowerRole.Shields)
                {
                    monitor.ResetConfirmation();
                    return false;
                }
                shieldsBoosted = true;
            }
            if (monitor.IsHeld(now)) return false;
            for (int i = 0; i < caster.Slots.Count && !shieldsBoosted; i++)
            {
                ShipAbilitySlot slot = caster.Slots[i];
                if (slot.CanActivate && ShipAbilityAutoUse.HasRule(slot.Definition) &&
                    _autoUse.ShouldActivate(caster, slot.Definition) &&
                    _activate(caster, slot.Id, null)) return true;
            }
            if (automation == null || !automation.Eligible) return false;
            AutoPowerSituation situation = _combat.Assess(caster, automation, shieldLoss, IsTravelling(entity));
            AutoPowerRole decision = Choose(caster, situation, shieldsBoosted, out _, out _);
            // A drained shield with a warship in reach cannot wait for a second check.
            bool urgent = situation.Urgent && decision == AutoPowerRole.Shields;
            if (!urgent && !monitor.Confirm(decision)) return false;
            monitor.ResetConfirmation();
            // Recheck right before spending: the target or readiness can change between checks.
            return Choose(caster, situation, shieldsBoosted, out ShipAbilitySlot chosen, out IEntity target) == decision &&
                   decision != AutoPowerRole.None && _activate(caster, chosen.Id, target);
        }

        // First wanted role with a ready slot; a recovering power never swaps Shields for Weapons or back.
        // While shields are boosted only Weapons may be added on top.
        private AutoPowerRole Choose(IShipAbilityFacade caster, AutoPowerSituation situation, bool weaponsOnly,
            out ShipAbilitySlot chosen, out IEntity target)
        {
            foreach (AutoPowerRole role in AutoPowerSituation.Priority)
            {
                if (!situation.Wants(role) || weaponsOnly && role != AutoPowerRole.Weapons) continue;
                foreach (ShipAbilitySlot slot in caster.Slots)
                {
                    if (slot.Definition.AutoPower != role || !slot.CanActivate) continue;
                    target = slot.Definition.RequiresEnemyTarget ? _combat.FindTarget(caster, slot.Definition) : null;
                    if (slot.Definition.RequiresEnemyTarget && target == null) continue;
                    chosen = slot;
                    return role;
                }
            }
            chosen = null;
            target = null;
            return AutoPowerRole.None;
        }

        private static bool IsTravelling(IEntity entity)
        {
            UnitOrderType order = entity.GetFacade<IUnitOrderObserverFacade>().CurrentOrder;
            return order == UnitOrderType.Move || order == UnitOrderType.WaypointMove || order == UnitOrderType.Retreat;
        }

        private AutoPowerMonitor GetMonitor(long id)
        {
            if (!_monitors.TryGetValue(id, out AutoPowerMonitor monitor))
            {
                monitor = new AutoPowerMonitor(_tuning);
                _monitors.Add(id, monitor);
            }
            return monitor;
        }
    }
}
