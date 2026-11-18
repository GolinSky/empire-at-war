using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Vision;

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
        private readonly IVisionService _vision;

        private readonly List<ShipAbilitySlot> _running = new List<ShipAbilitySlot>();
        private readonly List<IEntity> _pendingCasters = new List<IEntity>();
        private readonly Action<IReadOnlyList<IEntity>, ShipAbilityId, IEntity, bool, bool> _requestExecution;
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
        public ShipAbilityService(IShipAbilityFactory shipAbilityFactory, IPlayerRelations relations, IVisionService vision)
        {
            _shipAbilityFactory = shipAbilityFactory;
            _relations = relations;
            _vision = vision;
            _requestExecution = Execute;
        }

        /// <summary>Battle: player requests become <see cref="ShipAbilityCommand"/>s from the local player.</summary>
        [Inject]
        public ShipAbilityService(IShipAbilityFactory shipAbilityFactory, IPlayerRelations relations, IVisionService vision,
            IPlayerCommandSink commands, ILocalPlayer localPlayer, PlayerCommandHandlers commandHandlers)
        {
            _shipAbilityFactory = shipAbilityFactory;
            _relations = relations;
            _vision = vision;
            _commandHandlers = commandHandlers;
            _requestExecution = (casters, id, target, cancel, automatic) =>
                commands.Submit(ShipAbilityCommand.Create(localPlayer.Id, id, casters, target, cancel, automatic));
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
                _requestExecution(casters, id, null, true, false);
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
            if (accepted) _requestExecution(casters, id, null, false, false);
            else TargetRejected?.Invoke(casters);
        }

        public bool IsValidTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return false;
            for (int i = 0; i < _pendingCasters.Count; i++)
            {
                IEntity entity = _pendingCasters[i];
                if (entity.TryGetFacade(out IShipAbilityFacade command) &&
                    TryFindUsableSlot(command, PendingAbilityId, target, out ShipAbilitySlot slot) &&
                    (IsInRange(slot, target) || entity.TryGetFacade(out IShipAbilityCastFacade _)))
                    return true;
            }
            return false;
        }

        public void SubmitTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return;
            if (!IsValidTarget(target))
            {
                TargetRejected?.Invoke(_pendingCasters);
                return;
            }

            _requestExecution(_pendingCasters, PendingAbilityId, target, false, false);
            ClearTargeting();
        }

        /// <summary>
        /// Authority side of a player request: cancel, activate in place, or (with a target) send out-of-range
        /// casters to the target to use the ability on arrival.
        /// </summary>
        public void Execute(IReadOnlyList<IEntity> casters, ShipAbilityId id, IEntity target, bool cancel,
            bool automatic = false)
        {
            for (int i = 0; i < casters.Count; i++)
            {
                if (!casters[i].TryGetFacade(out IShipAbilityFacade command)) continue;
                if (cancel)
                {
                    TryCancel(command, id);
                    continue;
                }

                if (!TryFindSlot(command, id, out ShipAbilitySlot slot) || !slot.CanActivate) continue;
                if (automatic)
                {
                    IEntity automaticTarget = target;
                    if (slot.Definition.RequiresEnemyTarget)
                    {
                        IUnitOrderObserverFacade order = casters[i].GetFacade<IUnitOrderObserverFacade>();
                        if (order.CurrentOrder == UnitOrderType.Attack)
                        {
                            if (CanTarget(command, slot.Definition, order.AttackTarget) && IsInRange(slot, order.AttackTarget))
                                automaticTarget = order.AttackTarget;
                            else if (slot.Definition.IssuesAttackOrder)
                                continue;
                        }
                        if (!CanTarget(command, slot.Definition, automaticTarget) || !IsInRange(slot, automaticTarget))
                            continue;
                    }
                    Activate(slot, automaticTarget, automatic: true);
                    continue;
                }
                if (slot.Definition.RequiresEnemyTarget && !CanTarget(command, slot.Definition, target)) continue;
                if (IsInRange(slot, target)) Activate(slot, target);
                else if (casters[i].TryGetFacade(out IShipAbilityCastFacade cast))
                    cast.CastAbility(id, target, slot.Definition.Range);
            }
        }

        /// <summary>
        /// Automatic start for a player's ship: goes through the player command path like a press, so an online
        /// client asks the host instead of starting it only locally. False when it cannot start right now.
        /// </summary>
        public bool TryRequestActivation(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!CanActivateNow(caster, id, target)) return false;
            _requestExecution(new[] { caster.Entity }, id, target, false, true);
            return true;
        }

        private bool CanCancel(IShipAbilityFacade caster, ShipAbilityId id) =>
            TryFindSlot(caster, id, out ShipAbilitySlot slot) &&
            slot.CanCancel;

        private bool CanActivateNow(IShipAbilityFacade caster, ShipAbilityId id, IEntity target) =>
            TryFindUsableSlot(caster, id, target, out ShipAbilitySlot slot) && IsInRange(slot, target);

        private static bool IsInRange(ShipAbilitySlot slot, IEntity target) =>
            !slot.Definition.RequiresEnemyTarget || ShipAbilityRange.IsInRange(slot.Owner, target, slot.Definition.Range);

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
            if (!TryFindUsableSlot(caster, id, target, out ShipAbilitySlot slot) || !IsInRange(slot, target)) return false;
            Activate(slot, target);
            return true;
        }

        private void Activate(ShipAbilitySlot slot, IEntity target, bool automatic = false)
        {
            Start(slot, slot.Owner, target, automatic: automatic);
            Activated?.Invoke(slot.Owner, slot.Id, target);
        }

        /// <summary>
        /// Online client: the host started this ability. Range and readiness were checked there; replicated
        /// positions and cooldowns can differ slightly, so only a slot that is already running is skipped.
        /// </summary>
        public void ActivateReplica(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!TryFindSlot(caster, id, out ShipAbilitySlot slot) || slot.State == ShipAbilityState.Active) return;
            if (_running.Contains(slot)) _running.Remove(slot);
            Start(slot, caster, target, replica: true);
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

        private void Start(ShipAbilitySlot slot, IShipAbilityFacade caster, IEntity target,
            bool automatic = false, bool replica = false)
        {
            IShipAbility ability = _shipAbilityFactory.Create(slot.Definition);
            if (ability is ConcentrateFireAbility concentrateFire)
                concentrateFire.Start(caster, target, automatic, issueOrders: !replica);
            else
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
                var interruption = slot.RunningAbility as IShipAbilityInterruptionPolicy;
                if (slot.Owner.Health.IsDestroyed && (interruption == null || !interruption.SurvivesCasterDeath))
                {
                    if (slot.State == ShipAbilityState.Active) Stop(slot);
                    _running.RemoveAt(i);
                    continue;
                }
                // Baselines do not replay transient phases. Only the host decides when their active state ends.
                if (slot.IsReplicaState)
                {
                    if (slot.RunningAbility is IAdvancingShipAbility replica && !replica.IsComplete)
                        replica.Advance(deltaTime);
                    slot.Elapse(deltaTime);
                    continue;
                }
                var completion = slot.RunningAbility as ICompletableShipAbility;

                var modifiers = slot.Owner.Modifiers;
                // Completable effects decide how ion disable affects their current phase.
                if (slot.State == ShipAbilityState.Active &&
                    (modifiers.IsIonDisabled && completion == null ||
                     modifiers.IsAbilitySuppressed && (interruption == null || !interruption.SurvivesHazardSuppression)))
                {
                    Stop(slot);
                    continue;
                }

                if (slot.State == ShipAbilityState.Active && completion != null)
                {
                    bool complete = completion.IsComplete;
                    if (!complete && completion is IAdvancingShipAbility advancing)
                    {
                        advancing.Advance(deltaTime);
                        complete = advancing.IsComplete;
                    }
                    if (complete)
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
            out ShipAbilitySlot slot) =>
            TryFindSlot(caster, id, out slot) && slot.CanActivate &&
            (!slot.Definition.RequiresEnemyTarget || CanTarget(caster, slot.Definition, target));

        public bool CanTarget(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            IsVisibleHostile(caster.Entity.Owner, target) &&
            (!(definition.Settings is IShipAbilityTargetRule rule) || rule.CanTarget(caster, target));

        public bool IsVisibleHostile(PlayerId viewer, IEntity target) =>
            target != null && !target.HealthModel.IsDestroyed && !target.IsCloaked() &&
            _relations.IsHostile(viewer, target.Owner) &&
            (!_vision.FogEnabled || _vision.IsVisible(viewer, target));

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
