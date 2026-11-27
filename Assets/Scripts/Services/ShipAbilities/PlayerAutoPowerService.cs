using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>The local player's auto-power: picks its own eligible ships and runs the shared
    /// <see cref="AutoPowerDecider"/> on them. A manual ability press holds that ship for <see cref="AutoPowerTuning.ManualActionDelay"/>.</summary>
    public sealed class PlayerAutoPowerService : IPostTickable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly ISettingsService _settings;
        private readonly SkirmishStateModel _battle;
        private readonly ShipAbilityService _abilities;
        private readonly AutoPowerTuning _tuning;
        private readonly AutoPowerDecider _decider;
        private readonly List<IEntity> _casters = new List<IEntity>();
        private int _nextCaster;
        private float _now;
        private float _nextDecision;

        public PlayerAutoPowerService(IEntityLocator entities, ILocalPlayer player, ISettingsService settings,
            SkirmishStateModel battle, ShipAbilityService abilities,
            AutoPowerCombatSnapshot combat, ShipAbilityCatalog catalog, ShipAbilityAutoUse autoUse)
        {
            _entities = entities;
            _player = player;
            _settings = settings;
            _battle = battle;
            _abilities = abilities;
            _tuning = catalog.AutoPower;
            _decider = new AutoPowerDecider(abilities, combat, autoUse, _tuning, abilities.TryRequestActivation,
                abilities.TryRequestCancel);
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
                _decider.SuspendAll();
                _casters.Clear(); _nextCaster = 0;
                return;
            }
            if (_nextCaster >= _casters.Count && _now >= _nextDecision)
            {
                _casters.Clear(); _nextCaster = 0;
                foreach (IEntity entity in _entities.Entities)
                    if (CanMonitor(entity, out _, out _)) _casters.Add(entity);
                _decider.Capture();
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

        internal static bool HasAutoUseRule(IShipAbilityFacade caster)
        {
            for (int i = 0; i < caster.Slots.Count; i++)
                if (ShipAbilityAutoUse.HasRule(caster.Slots[i].Definition)) return true;
            return false;
        }

        private void Check(IEntity entity)
        {
            if (!CanMonitor(entity, out IShipAbilityFacade caster, out IShipAutoPowerFacade automation)) return;
            if (automation != null && !automation.Enabled)
            {
                _decider.Suspend(entity.Id);
                AutoPowerTelemetry.Record(entity, caster, automation, AutoPowerReason.ShipToggleOff, AutoPowerRole.None,
                    ShipAbilityId.None, AutoPowerShieldLoss.None, null);
                return;
            }
            _decider.Check(entity, caster, automation, _now);
        }

        private void HandleManualAction(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity entity in casters)
                if (_player.IsLocal(entity.Owner)) _decider.HoldManual(entity.Id, _now);
        }

        private void HandleRemoved(IEntity entity) => _decider.Remove(entity.Id);

        public void LateDispose()
        {
            _abilities.ManualAction -= HandleManualAction;
            _entities.EntityRemoved -= HandleRemoved;
            _decider.Clear(); _casters.Clear();
        }
    }
}
