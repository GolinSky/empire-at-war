using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;
using EmpireAtWar.Services.PlayerCommands;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Runs ship abilities and the local player's ability targeting. Player presses and target picks are decided
    /// here for UI feedback, then applied through <see cref="IPlayerCommandSink"/> (battle) or directly (standalone).
    /// </summary>
    public sealed class ShipAbilityService : ITickable, ILateDisposable, IShipAbilityTargeting, IShipAbilityCommandHandler
    {
        private readonly IShipAbilityFactory _shipAbilityFactory;
        private readonly IPlayerRelations _relations;

        private readonly List<ShipAbilitySlot> _running = new List<ShipAbilitySlot>();
        private readonly List<IEntity> _pendingCasters = new List<IEntity>();
        private readonly Action<IReadOnlyList<IEntity>, ShipAbilityId, IEntity, bool> _requestExecution;
        private readonly PlayerCommandHandlers _commandHandlers;

        public event Action TargetingChanged;
        public event Action<IReadOnlyList<IEntity>> ManualAction;
        public event Action<IReadOnlyList<IEntity>> TargetRejected;
        public event Action<IReadOnlyList<IEntity>> TargetingCanceled;
        /// <summary>Any ability start (players and AI); the online host relays it to clients.</summary>
        public event Action<IShipAbilityFacade, ShipAbilityId, IEntity> Activated;
        public event Action<IShipAbilityFacade, ShipAbilityId> Canceled;
        public event Action<ShipAbilitySlot> StateChanged;

        public bool IsWaitingForTarget { get; private set; }
        public ShipAbilityId PendingAbilityId { get; private set; }
        public IReadOnlyList<IEntity> PendingCasters => _pendingCasters;

        /// <summary>Standalone: player requests execute at once on this instance.</summary>
        public ShipAbilityService(IShipAbilityFactory shipAbilityFactory, IPlayerRelations relations)
        {
            _shipAbilityFactory = shipAbilityFactory;
            _relations = relations;
            _requestExecution = Execute;
        }

        /// <summary>Battle: player requests become <see cref="ShipAbilityCommand"/>s from the local player.</summary>
        [Inject]
        public ShipAbilityService(IShipAbilityFactory shipAbilityFactory, IPlayerRelations relations,
            IPlayerCommandSink commands, ILocalPlayer localPlayer, PlayerCommandHandlers commandHandlers)
        {
            _shipAbilityFactory = shipAbilityFactory;
            _relations = relations;
            _commandHandlers = commandHandlers;
            _requestExecution = (casters, id, target, cancel) =>
                commands.Submit(ShipAbilityCommand.Create(localPlayer.Id, id, casters, target, cancel));
            commandHandlers.RegisterAbilities(this);
        }

        public void LateDispose()
        {
            Clear();
            if (_commandHandlers != null) _commandHandlers.UnregisterAbilities();
        }

        public void Clear()
        {
            ClearTargeting();
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                if (_running[i].State == ShipAbilityState.Active) Stop(_running[i]);
            }
            _running.Clear();
        }

        public void Press(IReadOnlyList<IEntity> casters, ShipAbilityId id, bool allowCancel = true)
        {
            ManualAction?.Invoke(casters);
            if (allowCancel && casters.Count == 1 &&
                casters[0].TryGetFacade(out IShipAbilityFacade caster) && CanCancel(caster, id))
            {
                _requestExecution(casters, id, null, true);
                CancelTargeting();
                return;
            }

            ShipAbilityDefinition definition = null;
            for (int i = 0; i < casters.Count; i++)
            {
                if (casters[i].TryGetFacade(out IShipAbilityFacade command) &&
                    TryFindSlot(command, id, out ShipAbilitySlot slot) &&
                    slot.CanActivate)
                {
                    definition = slot.Definition;
                    break;
                }
            }
            if (definition == null)
            {
                TargetRejected?.Invoke(casters);
                return;
            }

            if (definition.RequiresEnemyTarget)
            {
                _pendingCasters.Clear();
                for (int i = 0; i < casters.Count; i++) _pendingCasters.Add(casters[i]);
                PendingAbilityId = id;
                IsWaitingForTarget = true;
                TargetingChanged?.Invoke();
                return;
            }

            CancelTargeting();
            bool accepted = false;
            for (int i = 0; i < casters.Count && !accepted; i++)
            {
                if (casters[i].TryGetFacade(out IShipAbilityFacade command))
                    accepted = CanActivateNow(command, id, null);
            }
            if (accepted) _requestExecution(casters, id, null, false);
            else TargetRejected?.Invoke(casters);
        }

        public bool IsValidTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return false;
            for (int i = 0; i < _pendingCasters.Count; i++)
            {
                // Out-of-range casters still count: SubmitTarget sends them to the target.
                if (_pendingCasters[i].TryGetFacade(out IShipAbilityFacade command) &&
                    TryFindUsableSlot(command, PendingAbilityId, target, out _))
                    return true;
            }
            return false;
        }

        public void SubmitTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return;
            bool accepted = false;
            for (int i = 0; i < _pendingCasters.Count && !accepted; i++)
            {
                if (!_pendingCasters[i].TryGetFacade(out IShipAbilityFacade command)) continue;
                accepted = CanActivateNow(command, PendingAbilityId, target) ||
                    TryFindUsableSlot(command, PendingAbilityId, target, out _) &&
                    _pendingCasters[i].TryGetFacade(out IShipAbilityCastFacade _);
            }
            if (!accepted)
            {
                TargetRejected?.Invoke(_pendingCasters);
                return;
            }

            _requestExecution(_pendingCasters, PendingAbilityId, target, false);
            ClearTargeting();
        }

        /// <summary>
        /// Authority side of a player request: cancel, activate in place, or (with a target) send out-of-range
        /// casters to the target to use the ability on arrival.
        /// </summary>
        public void Execute(IReadOnlyList<IEntity> casters, ShipAbilityId id, IEntity target, bool cancel)
        {
            for (int i = 0; i < casters.Count; i++)
            {
                if (!casters[i].TryGetFacade(out IShipAbilityFacade command)) continue;
                if (cancel)
                {
                    TryCancel(command, id);
                    continue;
                }

                if (TryActivate(command, id, target) || target == null) continue;
                // Out-of-range casters fly to the target and use the ability on arrival.
                if (TryFindUsableSlot(command, id, target, out ShipAbilitySlot slot) &&
                    casters[i].TryGetFacade(out IShipAbilityCastFacade cast))
                    cast.CastAbility(id, target, slot.Definition.Range);
            }
        }

        private bool CanCancel(IShipAbilityFacade caster, ShipAbilityId id) =>
            TryFindSlot(caster, id, out ShipAbilitySlot slot) &&
            slot.State == ShipAbilityState.Active && slot.Definition.CanCancel;

        private bool CanActivateNow(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!TryFindUsableSlot(caster, id, target, out ShipAbilitySlot slot)) return false;
            return !slot.Definition.RequiresEnemyTarget || PlanarGeometry.Distance(caster.WorldPosition,
                target.GetFacade<IEntityTransformFacade>().Transform.position) <= slot.Definition.Range;
        }

        public void CancelTargeting()
        {
            if (!IsWaitingForTarget) return;
            TargetingCanceled?.Invoke(_pendingCasters);
            ClearTargeting();
        }

        private void ClearTargeting()
        {
            if (!IsWaitingForTarget) return;
            IsWaitingForTarget = false;
            PendingAbilityId = ShipAbilityId.None;
            _pendingCasters.Clear();
            TargetingChanged?.Invoke();
        }

        public void PruneTargeting(IReadOnlyList<IEntity> selection)
        {
            if (!IsWaitingForTarget) return;
            _pendingCasters.RemoveAll(caster => caster.HealthModel.IsDestroyed ||
                !Contains(selection, caster) ||
                !caster.TryGetFacade(out IShipAbilityFacade abilities) ||
                !TryFindSlot(abilities, PendingAbilityId, out ShipAbilitySlot slot) || !slot.CanActivate);
            if (_pendingCasters.Count == 0) ClearTargeting();
            else TargetingChanged?.Invoke();
        }

        private static bool Contains(IReadOnlyList<IEntity> entities, IEntity entity)
        {
            for (int i = 0; i < entities.Count; i++)
                if (entities[i] == entity) return true;
            return false;
        }

        /// <summary>
        /// Host AI takeover: these entities are about to be rebuilt under the same ids. Their own abilities end with
        /// the old entities; abilities aimed at them stop (clients are told through <see cref="Canceled"/>).
        /// </summary>
        public void StopForRebuild(ICollection<long> rebuiltIds)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                ShipAbilitySlot slot = _running[i];
                bool ownRebuilt = rebuiltIds.Contains(slot.Owner.Entity.Id);
                bool aimedAtRebuilt = slot.State == ShipAbilityState.Active &&
                    slot.RunningAbility is ITargetedShipAbility targeted && rebuiltIds.Contains(targeted.Target.Id);
                if (!ownRebuilt && !aimedAtRebuilt) continue;
                if (slot.State == ShipAbilityState.Active)
                {
                    Stop(slot);
                    Canceled?.Invoke(slot.Owner, slot.Id);
                }

                if (ownRebuilt) _running.RemoveAt(i);
            }
        }

        public bool TryCancel(IShipAbilityFacade caster, ShipAbilityId id)
        {
            if (!TryFindSlot(caster, id, out ShipAbilitySlot slot) ||
                slot.State != ShipAbilityState.Active || !slot.Definition.CanCancel) return false;
            Stop(slot);
            Canceled?.Invoke(caster, id);
            return true;
        }

        public bool TryActivate(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!TryFindUsableSlot(caster, id, target, out ShipAbilitySlot slot)) return false;
            ShipAbilityDefinition definition = slot.Definition;
            if (definition.RequiresEnemyTarget &&
                PlanarGeometry.Distance(caster.WorldPosition, target.GetFacade<IEntityTransformFacade>().Transform.position) > definition.Range)
                return false;

            Start(slot, caster, target);
            Activated?.Invoke(caster, id, target);
            return true;
        }

        /// <summary>
        /// Online client: the host started this ability. Range and readiness were checked there; replicated
        /// positions and cooldowns can differ slightly, so only a slot that is already running is skipped.
        /// </summary>
        public void ActivateReplica(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!TryFindSlot(caster, id, out ShipAbilitySlot slot) || slot.State == ShipAbilityState.Active) return;
            if (_running.Contains(slot)) _running.Remove(slot);
            Start(slot, caster, target);
        }

        /// <summary>Online client: the host canceled this ability.</summary>
        public void CancelReplica(IShipAbilityFacade caster, ShipAbilityId id)
        {
            if (TryFindSlot(caster, id, out ShipAbilitySlot slot) && slot.State == ShipAbilityState.Active) Stop(slot);
        }

        public void RestoreReplicaState(IShipAbilityFacade caster, ShipAbilityId id, ShipAbilityState state,
            float timeLeft, IEntity target)
        {
            if (!TryFindSlot(caster, id, out ShipAbilitySlot slot)) return;
            IShipAbility running = slot.RunningAbility;
            if (state != ShipAbilityState.Active)
            {
                if (running is IStoppableShipAbility stoppable) stoppable.Stop();
                running = null;
            }
            else if (slot.State != ShipAbilityState.Active)
            {
                IShipAbility ability = _shipAbilityFactory.Create(slot.Definition);
                if (ability is IReplicaPersistentShipAbility)
                {
                    ability.Start(caster, slot.Definition, target);
                    running = ability;
                }
            }
            slot.RestoreReplicaState(state, timeLeft, running);
            _running.Remove(slot);
            if (state != ShipAbilityState.Ready) _running.Add(slot);
        }

        private void Start(ShipAbilitySlot slot, IShipAbilityFacade caster, IEntity target)
        {
            IShipAbility ability = _shipAbilityFactory.Create(slot.Definition);
            ability.Start(caster, slot.Definition, target);
            slot.Activate(ability);
            _running.Add(slot);
            StateChanged?.Invoke(slot);
        }

        /// <summary>Loads a saved slot state; a recovering slot resumes counting down.</summary>
        public void Restore(ShipAbilitySlot slot, ShipAbilityState state, float timeLeft)
        {
            slot.Restore(state, timeLeft);
            if (state == ShipAbilityState.Recovering && !_running.Contains(slot)) _running.Add(slot);
            StateChanged?.Invoke(slot);
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                ShipAbilitySlot slot = _running[i];
                // Baselines do not replay transient phases. Only the host decides when their active state ends.
                if (slot.IsReplicaState)
                {
                    if (slot.RunningAbility is IPhasedShipAbility replica && !replica.IsComplete)
                        replica.Advance(deltaTime);
                    slot.Elapse(deltaTime);
                    continue;
                }
                if (slot.Owner.Health.IsDestroyed &&
                    !(slot.State == ShipAbilityState.Active && slot.RunningAbility is IPhasedShipAbility persistent && persistent.SurvivesCasterDeath))
                {
                    if (slot.State == ShipAbilityState.Active) Stop(slot);
                    _running.RemoveAt(i);
                    continue;
                }

                if (slot.State == ShipAbilityState.Active && slot.Owner.Modifiers.AbilitiesSuppressed &&
                    (!(slot.RunningAbility is IPhasedShipAbility) ||
                     slot.Owner.Modifiers.IsAbilitySuppressed &&
                     !((IPhasedShipAbility)slot.RunningAbility).SurvivesCasterDeath))
                {
                    Stop(slot);
                    continue;
                }

                if (slot.State == ShipAbilityState.Active && slot.RunningAbility is IPhasedShipAbility phased)
                {
                    if (phased.IsComplete)
                    {
                        Stop(slot);
                        continue;
                    }
                    phased.Advance(deltaTime);
                    if (phased.IsComplete)
                    {
                        Stop(slot);
                        continue;
                    }
                    if (slot.Definition.Duration == 0f) continue;
                }

                if (slot.State == ShipAbilityState.Active && slot.Definition.IsToggle) continue;

                slot.Elapse(deltaTime);
                if (slot.TimeLeft > 0f) continue;
                if (slot.State == ShipAbilityState.Active)
                {
                    Stop(slot);
                    if (slot.TimeLeft > 0f) continue;
                }

                // A Ready slot must leave the running list, otherwise a re-activation adds it twice.
                slot.Ready();
                StateChanged?.Invoke(slot);
                _running.RemoveAt(i);
            }
        }

        private void Stop(ShipAbilitySlot slot)
        {
            IShipAbility ability = slot.RunningAbility;
            slot.Recover();
            if (ability is IStoppableShipAbility stoppable) stoppable.Stop();
            StateChanged?.Invoke(slot);
        }

        private bool TryFindUsableSlot(IShipAbilityFacade caster, ShipAbilityId id, IEntity target,
            out ShipAbilitySlot slot)
        {
            slot = null;
            if (caster.Modifiers.IsCloaked && id != ShipAbilityId.Cloak) return false;
            if (!TryFindSlot(caster, id, out slot) || !slot.CanActivate) return false;
            return !slot.Definition.RequiresEnemyTarget ||
                   (target != null && !target.HealthModel.IsDestroyed && !target.IsCloaked() &&
                    _relations.IsHostile(caster.Entity.Owner, target.Owner) &&
                    (!(slot.Definition.Settings is IShipAbilityTargetRule rule) || rule.CanTarget(caster, target)));
        }

        private static bool TryFindSlot(IShipAbilityFacade caster, ShipAbilityId id, out ShipAbilitySlot slot)
        {
            for (int i = 0; i < caster.Slots.Count; i++)
            {
                if (caster.Slots[i].Id == id)
                {
                    slot = caster.Slots[i];
                    return true;
                }
            }

            slot = null;
            return false;
        }
    }
}
