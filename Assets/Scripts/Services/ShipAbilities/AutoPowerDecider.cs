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
        private readonly Func<IShipAbilityFacade, ShipAbilityId, bool> _cancel;
        private readonly Dictionary<long, AutoPowerMonitor> _monitors = new Dictionary<long, AutoPowerMonitor>();

        /// <param name="activate">Starts the ability: a request from a player, a direct start for the host AI.</param>
        /// <param name="cancel">Ends a lasting ability whose stop rule says so, the same way.</param>
        public AutoPowerDecider(ShipAbilityService abilities, AutoPowerCombatSnapshot combat, ShipAbilityAutoUse autoUse,
            AutoPowerTuning tuning, Func<IShipAbilityFacade, ShipAbilityId, IEntity, bool> activate,
            Func<IShipAbilityFacade, ShipAbilityId, bool> cancel)
        {
            _abilities = abilities;
            _combat = combat;
            _autoUse = autoUse;
            _tuning = tuning;
            _activate = activate;
            _cancel = cancel;
        }

        public void Capture() => _combat.Capture();

        public void Suspend(long id) => GetMonitor(id).Suspend();

        public void SuspendAll()
        {
            foreach (AutoPowerMonitor monitor in _monitors.Values) monitor.Suspend();
        }

        public void HoldManual(long id, float now) => GetMonitor(id).HoldManual(now);

        public void Remove(long id)
        {
            _monitors.Remove(id);
            AutoPowerTelemetry.Forget(id);
        }

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
            monitor.MarkSeen(now);
            AutoPowerShieldLoss shieldLoss = automation == null ? AutoPowerShieldLoss.None
                : monitor.Sample(now, caster.Health.Shields, automation.MaxShields, automation.ShieldSuppressed,
                    automation.ShieldRevision);
            // A lasting ability ends once its stop rule says so (S-foils reopen when travel ends); never under a
            // manual hold, so a hand-set toggle stays as the player left it for the hold time.
            if (!monitor.IsHeld(now))
                foreach (ShipAbilitySlot slot in caster.Slots)
                    if (slot.State == ShipAbilityState.Active && _autoUse.ShouldStop(caster, slot.Definition) &&
                        _cancel(caster, slot.Id))
                        return Report(entity, caster, automation, AutoPowerReason.StopRuleCancelled, slot, shieldLoss, true);
            // Weapons may stack on an active shield boost, and a shield boost on any other active ability;
            // every other role waits while an ability runs.
            bool shieldsBoosted = false;
            bool otherActive = false;
            foreach (ShipAbilitySlot slot in caster.Slots)
            {
                if (slot.State != ShipAbilityState.Active) continue;
                if (slot.Definition.AutoPower == AutoPowerRole.Shields) shieldsBoosted = true;
                else otherActive = true;
            }
            if (shieldsBoosted && otherActive)
            {
                monitor.ResetConfirmation();
                return Report(entity, caster, automation, AutoPowerReason.AbilitiesRunning, null, shieldLoss, false);
            }
            if (monitor.IsHeld(now))
                return Report(entity, caster, automation, AutoPowerReason.ManualHold, null, shieldLoss, false);
            for (int i = 0; i < caster.Slots.Count && !shieldsBoosted && !otherActive; i++)
            {
                ShipAbilitySlot slot = caster.Slots[i];
                if (!slot.CanActivate || !ShipAbilityAutoUse.HasRule(slot.Definition) ||
                    !monitor.IsWarmedUp(now, slot.Definition.AutoUseStartDelay) ||
                    !_autoUse.ShouldActivate(caster, slot.Definition)) continue;
                // A targeted rule ability takes the same target an auto-power role would.
                IEntity ruleTarget = slot.Definition.RequiresEnemyTarget ? _combat.FindTarget(caster, slot.Definition) : null;
                if ((ruleTarget != null || !slot.Definition.RequiresEnemyTarget) && _activate(caster, slot.Id, ruleTarget))
                    return Report(entity, caster, automation, AutoPowerReason.RuleActivated, slot, shieldLoss, true);
            }
            if (automation == null || !automation.Eligible)
                return Report(entity, caster, automation, AutoPowerReason.NoAutoPowerRoles, null, shieldLoss, false);
            AutoPowerSituation situation = _combat.Assess(caster, automation, shieldLoss, IsTravelling(entity));
            AutoPowerRole only = shieldsBoosted ? AutoPowerRole.Weapons
                : otherActive ? AutoPowerRole.Shields : AutoPowerRole.None;
            AutoPowerRole decision = Choose(caster, monitor, now, situation, only, out _, out _, out AutoPowerRole wanted);
            if (decision == AutoPowerRole.None)
            {
                monitor.Confirm(AutoPowerRole.None);
                return Report(entity, caster, automation, wanted == AutoPowerRole.None
                    ? AutoPowerReason.NothingWanted : AutoPowerReason.WantedNotReady, wanted, null, shieldLoss, situation, false);
            }
            // A drained shield with a warship in reach cannot wait for a second check.
            bool urgent = situation.Urgent && decision == AutoPowerRole.Shields;
            if (!urgent && !monitor.Confirm(decision))
                return Report(entity, caster, automation, AutoPowerReason.AwaitingConfirmation, decision, null,
                    shieldLoss, situation, false);
            monitor.ResetConfirmation();
            // Recheck right before spending: the target or readiness can change between checks.
            if (Choose(caster, monitor, now, situation, only, out ShipAbilitySlot chosen, out IEntity target, out _) != decision)
                return Report(entity, caster, automation, AutoPowerReason.RecheckChanged, decision, null,
                    shieldLoss, situation, false);
            bool started = _activate(caster, chosen.Id, target);
            return Report(entity, caster, automation, started ? AutoPowerReason.Activated : AutoPowerReason.ActivationRefused,
                decision, chosen, shieldLoss, situation, started);
        }

        private static bool Report(IEntity entity, IShipAbilityFacade caster, IShipAutoPowerFacade automation,
            AutoPowerReason reason, ShipAbilitySlot slot, AutoPowerShieldLoss shieldLoss, bool started) =>
            Report(entity, caster, automation, reason, slot?.Definition.AutoPower ?? AutoPowerRole.None, slot, shieldLoss,
                null, started);

        // Every exit of a check reports its reason; the telemetry keeps only changes per caster.
        private static bool Report(IEntity entity, IShipAbilityFacade caster, IShipAutoPowerFacade automation,
            AutoPowerReason reason, AutoPowerRole role, ShipAbilitySlot slot, AutoPowerShieldLoss shieldLoss,
            AutoPowerSituation? situation, bool started)
        {
            AutoPowerTelemetry.Record(entity, caster, automation, reason, role, slot?.Id ?? ShipAbilityId.None,
                shieldLoss, situation);
            return started;
        }

        // First wanted role with a ready slot; a recovering power never swaps Shields for Weapons or back.
        // only: the one role allowed beside an active ability (None = any). A slot inside its start delay is skipped.
        // wanted: the first role the situation asks for, ready or not.
        private AutoPowerRole Choose(IShipAbilityFacade caster, AutoPowerMonitor monitor, float now,
            AutoPowerSituation situation, AutoPowerRole only, out ShipAbilitySlot chosen, out IEntity target,
            out AutoPowerRole wanted)
        {
            wanted = AutoPowerRole.None;
            foreach (AutoPowerRole role in AutoPowerSituation.Priority)
            {
                if (!situation.Wants(role) || only != AutoPowerRole.None && role != only) continue;
                if (wanted == AutoPowerRole.None) wanted = role;
                foreach (ShipAbilitySlot slot in caster.Slots)
                {
                    if (slot.Definition.AutoPower != role || !slot.CanActivate ||
                        !monitor.IsWarmedUp(now, slot.Definition.AutoUseStartDelay)) continue;
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
