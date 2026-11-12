using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class PlayerAutoPowerService : IPostTickable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly ISettingsService _settings;
        private readonly ISelectionService _selection;
        private readonly SkirmishStateModel _battle;
        private readonly ShipAbilityService _abilities;
        private readonly AutoPowerCombatSnapshot _combat;
        private readonly AutoPowerTuning _tuning;
        private readonly ShipAbilityAutoUse _autoUse;
        private readonly Dictionary<long, AutoPowerMonitor> _monitors = new Dictionary<long, AutoPowerMonitor>();
        private readonly List<IEntity> _casters = new List<IEntity>();
        private int _nextCaster;
        private float _now;
        private float _nextDecision;

        public PlayerAutoPowerService(IEntityLocator entities, ILocalPlayer player, ISettingsService settings,
            ISelectionService selection, SkirmishStateModel battle, ShipAbilityService abilities,
            AutoPowerCombatSnapshot combat, ShipAbilityCatalog catalog, ShipAbilityAutoUse autoUse)
        {
            _entities = entities;
            _player = player;
            _settings = settings;
            _selection = selection;
            _battle = battle;
            _abilities = abilities;
            _combat = combat;
            _tuning = catalog.AutoPower;
            _autoUse = autoUse;
            _abilities.ManualAction += HandleManualAction;
            _entities.EntityRemoved += HandleRemoved;
        }

        // UI input runs in MonoBehaviour.Update; manual intent must win before this frame's automatic starts.
        public void PostTick()
        {
            if (_battle.State == BattleState.Running) _now += Time.deltaTime;
            if (_battle.State != BattleState.Running || _player.Slot.Controller != PlayerController.Human ||
                !_settings.Saved.Gameplay.AutoPowerAbilities)
            {
                foreach (AutoPowerMonitor monitor in _monitors.Values) monitor.Suspend();
                _casters.Clear(); _nextCaster = 0;
                return;
            }
            if (_nextCaster >= _casters.Count && _now >= _nextDecision)
            {
                _casters.Clear(); _nextCaster = 0;
                foreach (IEntity entity in _entities.Entities)
                    if (CanMonitor(entity, out _, out _)) _casters.Add(entity);
                _combat.Capture();
                _nextDecision = _now + _tuning.DecisionInterval;
            }
            int end = Mathf.Min(_nextCaster + _tuning.CastersPerFrame, _casters.Count);
            for (; _nextCaster < end; _nextCaster++) Check(_casters[_nextCaster]);
        }

        private bool CanMonitor(IEntity entity, out IShipAbilityFacade caster, out IShipAutoPowerFacade automation)
        {
            caster = null; automation = null;
            return _entities.TryGetEntity(entity.Id, out IEntity registered) && ReferenceEquals(entity, registered) &&
                _player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                entity.TryGetFacade(out caster) &&
                (entity.TryGetFacade(out automation) && automation.Eligible || HasAutoUseRule(caster));
        }

        private static bool HasAutoUseRule(IShipAbilityFacade caster)
        {
            for (int i = 0; i < caster.Slots.Count; i++)
                if (ShipAbilityAutoUse.HasRule(caster.Slots[i].Definition)) return true;
            return false;
        }

        private void Check(IEntity entity)
        {
            if (!CanMonitor(entity, out IShipAbilityFacade caster, out IShipAutoPowerFacade automation)) return;
            if ((automation != null && !automation.Enabled) ||
                (_selection.PlayerSelectionContext.Count == 1 && _selection.PlayerSelectionContext.Contains(entity)))
            {
                GetMonitor(entity.Id).Suspend();
                return;
            }
            AutoPowerMonitor monitor = GetMonitor(entity.Id);
            AutoPowerShieldLoss shieldLoss = automation == null ? AutoPowerShieldLoss.None
                : monitor.Sample(_now, caster.Health.Shields, automation.MaxShields, automation.ShieldSuppressed,
                    automation.ShieldRevision);
            foreach (ShipAbilitySlot slot in caster.Slots)
                if (slot.State == ShipAbilityState.Active) { monitor.ResetConfirmation(); return; }
            if (monitor.IsHeld(_now)) return;
            for (int i = 0; i < caster.Slots.Count; i++)
            {
                ShipAbilitySlot slot = caster.Slots[i];
                if (slot.CanActivate && _autoUse.ShouldActivate(caster, slot.Definition) &&
                    _abilities.TryRequestActivation(caster, slot.Id, null)) return;
            }
            if (automation == null || !automation.Eligible) return;
            AutoPowerSituation situation = _combat.Assess(caster, automation, shieldLoss, IsTravelling(entity));
            AutoPowerRole decision = Choose(caster, situation, out _, out _);
            if (!monitor.Confirm(decision)) return;
            monitor.ResetConfirmation();
            // Recheck right before spending: the target or readiness can change between checks.
            if (Choose(caster, situation, out ShipAbilitySlot chosen, out IEntity target) == decision)
                _abilities.TryRequestActivation(caster, chosen.Id, target);
        }

        // First wanted role with a ready slot; a recovering power never swaps Shields for Weapons or back.
        private AutoPowerRole Choose(IShipAbilityFacade caster, AutoPowerSituation situation,
            out ShipAbilitySlot chosen, out IEntity target)
        {
            foreach (AutoPowerRole role in AutoPowerSituation.Priority)
            {
                if (!situation.Wants(role)) continue;
                foreach (ShipAbilitySlot slot in caster.Slots)
                {
                    if (slot.Definition.AutoPower != role || !slot.CanActivate) continue;
                    target = slot.Definition.RequiresEnemyTarget ? _combat.FindTarget(caster, slot.Definition) : null;
                    if (slot.Definition.RequiresEnemyTarget && target == null) continue;
                    chosen = slot;
                    return role;
                }
            }
            chosen = null; target = null;
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

        private void HandleManualAction(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity entity in casters)
                if (_player.IsLocal(entity.Owner)) GetMonitor(entity.Id).HoldManual(_now);
        }

        private void HandleRemoved(IEntity entity) => _monitors.Remove(entity.Id);

        public void LateDispose()
        {
            _abilities.ManualAction -= HandleManualAction;
            _entities.EntityRemoved -= HandleRemoved;
            _monitors.Clear(); _casters.Clear();
        }
    }
}
