using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Networking.Messages;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: one armed entity's current targets as messages — the base list and the main target — so a client that
    /// (re)joins gets them in its baseline. Each change is also handed to <c>send</c>.
    /// </summary>
    public sealed class WeaponTargetRecord
    {
        private const int NO_HARD_POINT = -1;
        // Base targets are only added, never removed; dead ones are dropped when the list grows past this.
        private const int PRUNE_AT = 32;

        private readonly long _shooterId;
        private readonly IWeaponTargetReplicaFacade _weapon;
        private readonly IEntityLocator _entities;
        private readonly Action<WeaponTargetMessage> _send;
        private readonly List<WeaponTargetMessage> _bases = new List<WeaponTargetMessage>();
        private WeaponTargetMessage? _main;

        public WeaponTargetRecord(long shooterId, IWeaponTargetReplicaFacade weapon, IEntityLocator entities,
            Action<WeaponTargetMessage> send)
        {
            _shooterId = shooterId;
            _weapon = weapon;
            _entities = entities;
            _send = send;
            _weapon.TargetAdded += OnTargetAdded;
            _weapon.TargetReset += OnTargetReset;
        }

        public void Dispose()
        {
            _weapon.TargetAdded -= OnTargetAdded;
            _weapon.TargetReset -= OnTargetReset;
        }

        /// <summary>Live targets in the order they were set: bases, then the main target.</summary>
        public void CollectBaseline(List<WeaponTargetMessage> messages)
        {
            Prune();
            messages.AddRange(_bases);
            if (_main.HasValue && _entities.TryGetEntity(_main.Value.TargetId, out _)) messages.Add(_main.Value);
        }

        private void OnTargetAdded(AttackData attackData, AttackType attackType)
        {
            WeaponTargetMessage message = new WeaponTargetMessage
            {
                ShooterId = _shooterId,
                TargetId = attackData.TargetEntity.Id,
                AttackType = attackType,
                HardPointId = attackData.IsFocused ? FindHardPoint(attackData) : NO_HARD_POINT,
                HardPointType = attackData.HardPointType
            };
            if (attackType == AttackType.MainTarget) _main = message;
            else
            {
                _bases.Add(message);
                if (_bases.Count > PRUNE_AT) Prune();
            }

            _send(message);
        }

        private void OnTargetReset()
        {
            _main = null;
            _send(new WeaponTargetMessage { ShooterId = _shooterId, Reset = true });
        }

        private void Prune() => _bases.RemoveAll(message => !_entities.TryGetEntity(message.TargetId, out _));

        private static int FindHardPoint(AttackData attackData)
        {
            var hardPoints = attackData.TargetEntity.GetFacade<IHardPointsFacade>().HardPoints;
            for (int i = 0; i < hardPoints.Count; i++)
                if (ReferenceEquals(hardPoints[i], attackData.Units[0])) return i;
            throw new InvalidOperationException(
                $"Focused hardpoint is not on target {attackData.TargetEntity.Id}.");
        }
    }
}
