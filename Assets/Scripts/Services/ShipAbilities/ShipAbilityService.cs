using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityService : ITickable, ILateDisposable, IShipAbilityTargeting
    {
        private readonly IShipAbilityFactory _factory;
        private readonly List<ShipAbilitySlot> _running = new List<ShipAbilitySlot>();
        private readonly List<IEntity> _pendingCasters = new List<IEntity>();

        public event Action TargetingChanged;
        public bool IsWaitingForTarget { get; private set; }
        public ShipAbilityId PendingAbilityId { get; private set; }

        public ShipAbilityService(IShipAbilityFactory factory) { _factory = factory; }

        public void Press(IReadOnlyList<IEntity> casters, ShipAbilityId id)
        {
            bool cancel = false;
            for (int i = 0; i < casters.Count; i++)
            {
                if (casters[i].TryGetCommand(out IShipAbilityCommand command) &&
                    FindSlot(command, id) is ShipAbilitySlot slot &&
                    slot.State == ShipAbilityState.Active && slot.Definition.CanCancel)
                {
                    cancel = true;
                    break;
                }
            }

            if (cancel)
            {
                for (int i = 0; i < casters.Count; i++)
                {
                    if (casters[i].TryGetCommand(out IShipAbilityCommand command) &&
                        FindSlot(command, id) is ShipAbilitySlot slot &&
                        slot.State == ShipAbilityState.Active && slot.Definition.CanCancel)
                        Stop(slot);
                }
                CancelTargeting();
                return;
            }

            ShipAbilityDefinition definition = null;
            for (int i = 0; i < casters.Count; i++)
            {
                if (casters[i].TryGetCommand(out IShipAbilityCommand command) &&
                    FindSlot(command, id) is ShipAbilitySlot slot &&
                    slot.State == ShipAbilityState.Ready && !command.Health.IsDestroyed)
                {
                    definition = slot.Definition;
                    break;
                }
            }
            if (definition == null) return;

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
            for (int i = 0; i < casters.Count; i++)
            {
                if (casters[i].TryGetCommand(out IShipAbilityCommand command))
                    TryActivate(command, id, null);
            }
        }

        public void SubmitTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return;
            bool activated = false;
            for (int i = 0; i < _pendingCasters.Count; i++)
            {
                if (_pendingCasters[i].TryGetCommand(out IShipAbilityCommand command))
                    activated |= TryActivate(command, PendingAbilityId, target);
            }
            if (activated) CancelTargeting();
        }

        public void CancelTargeting()
        {
            if (!IsWaitingForTarget) return;
            IsWaitingForTarget = false;
            PendingAbilityId = ShipAbilityId.None;
            _pendingCasters.Clear();
            TargetingChanged?.Invoke();
        }

        public bool TryActivate(IShipAbilityCommand caster, ShipAbilityId id, IEntity target)
        {
            ShipAbilitySlot slot = FindSlot(caster, id);
            if (slot == null || slot.State != ShipAbilityState.Ready || caster.Health.IsDestroyed)
                return false;
            ShipAbilityDefinition definition = slot.Definition;
            if (definition.RequiresEnemyTarget &&
                (target == null || target.HealthModel.IsDestroyed ||
                 target.PlayerType == caster.Entity.PlayerType ||
                 Vector3.Distance(caster.WorldPosition, target.HealthModel.Transform.position) > definition.Range))
                return false;

            IShipAbility ability = _factory.Create(definition);
            ability.Start(caster, definition, target);
            slot.Activate(ability);
            _running.Add(slot);
            return true;
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                ShipAbilitySlot slot = _running[i];
                if (slot.Owner.Health.IsDestroyed)
                {
                    if (slot.State == ShipAbilityState.Active) Stop(slot);
                    _running.RemoveAt(i);
                    continue;
                }

                slot.Elapse(deltaTime);
                if (slot.TimeLeft > 0f) continue;
                if (slot.State == ShipAbilityState.Active)
                {
                    Stop(slot);
                    if (slot.TimeLeft > 0f) continue;
                }

                // A Ready slot must leave the running list, otherwise a re-activation adds it twice.
                slot.Ready();
                _running.RemoveAt(i);
            }
        }

        public void LateDispose()
        {
            CancelTargeting();
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                if (_running[i].State == ShipAbilityState.Active) Stop(_running[i]);
            }
            _running.Clear();
        }

        private void Stop(ShipAbilitySlot slot)
        {
            IShipAbility ability = slot.RunningAbility;
            slot.Recover();
            ability.Stop();
        }

        private static ShipAbilitySlot FindSlot(IShipAbilityCommand caster, ShipAbilityId id)
        {
            for (int i = 0; i < caster.Slots.Count; i++)
                if (caster.Slots[i].Id == id) return caster.Slots[i];
            return null;
        }
    }
}
