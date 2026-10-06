using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityService : ITickable, ILateDisposable, IShipAbilityTargeting
    {
        private readonly IShipAbilityFactory _shipAbilityFactory;
        private readonly IPlayerRelations _relations;

        private readonly List<ShipAbilitySlot> _running = new List<ShipAbilitySlot>();
        private readonly List<IEntity> _pendingCasters = new List<IEntity>();

        public event Action TargetingChanged;

        public bool IsWaitingForTarget { get; private set; }
        public ShipAbilityId PendingAbilityId { get; private set; }

        public ShipAbilityService(IShipAbilityFactory shipAbilityFactory, IPlayerRelations relations)
        {
            _shipAbilityFactory = shipAbilityFactory;
            _relations = relations;
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

        public void Press(IReadOnlyList<IEntity> casters, ShipAbilityId id)
        {
            bool cancel = false;
            for (int i = 0; i < casters.Count; i++)
            {
                if (casters[i].TryGetFacade(out IShipAbilityFacade command) &&
                    TryFindSlot(command, id, out ShipAbilitySlot slot) &&
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
                    if (casters[i].TryGetFacade(out IShipAbilityFacade command) &&
                        TryFindSlot(command, id, out ShipAbilitySlot slot) &&
                        slot.State == ShipAbilityState.Active && slot.Definition.CanCancel)
                        Stop(slot);
                }
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
                if (casters[i].TryGetFacade(out IShipAbilityFacade command))
                    TryActivate(command, id, null);
            }
        }

        public void SubmitTarget(IEntity target)
        {
            if (!IsWaitingForTarget) return;
            bool accepted = false;
            for (int i = 0; i < _pendingCasters.Count; i++)
            {
                if (!_pendingCasters[i].TryGetFacade(out IShipAbilityFacade command)) continue;
                if (TryActivate(command, PendingAbilityId, target))
                {
                    accepted = true;
                    continue;
                }

                // Out-of-range casters fly to the target and use the ability on arrival.
                if (TryFindUsableSlot(command, PendingAbilityId, target, out ShipAbilitySlot slot) &&
                    CanStart(command, slot.Definition, target) &&
                    _pendingCasters[i].TryGetFacade(out IShipAbilityCastFacade cast))
                {
                    cast.CastAbility(PendingAbilityId, target, slot.Definition.Range);
                    accepted = true;
                }
            }
            if (accepted) CancelTargeting();
        }

        public void CancelTargeting()
        {
            if (!IsWaitingForTarget) return;
            IsWaitingForTarget = false;
            PendingAbilityId = ShipAbilityId.None;
            _pendingCasters.Clear();
            TargetingChanged?.Invoke();
        }

        public bool TryActivate(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!TryFindUsableSlot(caster, id, target, out ShipAbilitySlot slot)) return false;
            ShipAbilityDefinition definition = slot.Definition;
            if (definition.RequiresEnemyTarget &&
                PlanarGeometry.Distance(caster.WorldPosition, target.GetFacade<IEntityTransformFacade>().Transform.position) > definition.Range)
                return false;

            IShipAbility ability = _shipAbilityFactory.Create(definition);
            if (ability is IPhasedShipAbility phased && !phased.CanStart(caster, target)) return false;
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
                if (slot.Owner.Health.IsDestroyed &&
                    !(slot.State == ShipAbilityState.Active && slot.RunningAbility is IPhasedShipAbility persistent && persistent.SurvivesCasterDeath))
                {
                    if (slot.State == ShipAbilityState.Active) Stop(slot);
                    _running.RemoveAt(i);
                    continue;
                }

                if (slot.State == ShipAbilityState.Active && slot.Owner.Modifiers.IsIonDisabled &&
                    !(slot.RunningAbility is IPhasedShipAbility))
                {
                    Stop(slot);
                    continue;
                }

                if (slot.State == ShipAbilityState.Active && slot.RunningAbility is IPhasedShipAbility phased)
                {
                    phased.Advance(deltaTime);
                    if (phased.IsComplete)
                    {
                        Stop(slot);
                        continue;
                    }
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
                _running.RemoveAt(i);
            }
        }

        // Ineligible targets are rejected outright instead of sending the caster to them.
        private bool CanStart(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            !(_shipAbilityFactory.Create(definition) is IPhasedShipAbility phased) || phased.CanStart(caster, target);

        private void Stop(ShipAbilitySlot slot)
        {
            IShipAbility ability = slot.RunningAbility;
            slot.Recover();
            ability.Stop();
        }

        private bool TryFindUsableSlot(IShipAbilityFacade caster, ShipAbilityId id, IEntity target,
            out ShipAbilitySlot slot)
        {
            slot = null;
            if (caster.Modifiers.IsCloaked && id != ShipAbilityId.Cloak) return false;
            if (!TryFindSlot(caster, id, out slot) || !slot.CanActivate) return false;
            return !slot.Definition.RequiresEnemyTarget ||
                   (target != null && !target.HealthModel.IsDestroyed && !target.IsCloaked() &&
                    _relations.IsHostile(caster.Entity.Owner, target.Owner));
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
