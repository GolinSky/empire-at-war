using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.ShipAbilities.Abilities;
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
        private readonly Dictionary<long, AutoPowerMonitor> _monitors = new Dictionary<long, AutoPowerMonitor>();
        private readonly List<IEntity> _casters = new List<IEntity>();
        private int _nextCaster;
        private float _now;
        private float _nextDecision;

        public PlayerAutoPowerService(IEntityLocator entities, ILocalPlayer player, ISettingsService settings,
            ISelectionService selection, SkirmishStateModel battle, ShipAbilityService abilities,
            AutoPowerCombatSnapshot combat, ShipAbilityCatalog catalog)
        {
            _entities = entities; _player = player; _settings = settings; _selection = selection;
            _battle = battle; _abilities = abilities; _combat = combat; _tuning = catalog.AutoPower;
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
                entity.TryGetFacade(out automation) && automation.Eligible &&
                entity.TryGetFacade(out caster);
        }

        private void Check(IEntity entity)
        {
            if (!CanMonitor(entity, out IShipAbilityFacade caster, out IShipAutoPowerFacade automation)) return;
            if (!automation.Enabled || (_selection.PlayerSelectionContext.Count == 1 && _selection.PlayerSelectionContext.Contains(entity)))
            {
                GetMonitor(entity.Id).Suspend();
                return;
            }
            AutoPowerMonitor monitor = GetMonitor(entity.Id);
            bool heavyLoss = monitor.Sample(_now, caster.Health.Shields, automation.MaxShields, automation.ShieldSuppressed,
                _tuning, automation.ShieldRevision);
            foreach (ShipAbilitySlot slot in caster.Slots)
                if (slot.State == ShipAbilityState.Active) { monitor.ResetConfirmation(); return; }
            if (_now < monitor.ManualUntil) { monitor.ResetConfirmation(); return; }
            AutoPowerDecision decision = _combat.Decide(caster, automation, heavyLoss, _tuning);
            if (!monitor.Confirm(decision, _tuning.ConfirmationChecks)) return;
            foreach (ShipAbilitySlot slot in caster.Slots)
            {
                if (!AutoPowerEligibility.IsEligible(slot)) continue;
                bool matches = decision == AutoPowerDecision.Shields
                    ? slot.Definition.Settings is BoostShieldPowerSettings
                    : slot.Definition.Settings is BoostWeaponPowerSettings;
                if (!matches) continue;
                if (slot.CanActivate) _abilities.TryActivate(caster, slot.Id, null);
                monitor.ResetConfirmation();
                return;
            }
        }

        private AutoPowerMonitor GetMonitor(long id)
        {
            if (!_monitors.TryGetValue(id, out AutoPowerMonitor monitor))
            {
                monitor = new AutoPowerMonitor();
                _monitors.Add(id, monitor);
            }
            return monitor;
        }

        private void HandleManualAction(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity entity in casters)
                if (_player.IsLocal(entity.Owner))
                {
                    AutoPowerMonitor monitor = GetMonitor(entity.Id);
                    monitor.ManualUntil = _now + _tuning.ManualActionDelay;
                    monitor.ResetConfirmation();
                }
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
