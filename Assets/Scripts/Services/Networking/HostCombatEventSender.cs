using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: relays attack targets and ability starts/cancels (players and AI) so clients aim their weapons and
    /// play abilities like the host. Accepted order feedback goes only to its issuer. Damage stays on the host.
    /// </summary>
    public sealed class HostCombatEventSender : IInitializable, ILateDisposable
    {
        private const long NO_TARGET = 0;

        private readonly UnitOrderService _orders;
        private readonly ShipAbilityService _abilities;
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly IEntityLocator _entities;
        private readonly NetworkEventSequence _events = new NetworkEventSequence();
        private readonly Queue<(long Sequence, OrderFeedbackMessage Message)> _pendingOrders = new Queue<(long, OrderFeedbackMessage)>();
        private readonly Queue<(long Sequence, AttackTargetMessage Message)> _pendingAttacks = new Queue<(long, AttackTargetMessage)>();
        private readonly Queue<(long Sequence, ShipAbilityEventMessage Message)> _pendingAbilities = new Queue<(long, ShipAbilityEventMessage)>();

        public HostCombatEventSender(UnitOrderService orders, ShipAbilityService abilities, OnlineMatchService online,
            NetworkTickService ticks, IEntityLocator entities)
        {
            _orders = orders;
            _abilities = abilities;
            _online = online;
            _ticks = ticks;
            _entities = entities;
        }

        public void Initialize()
        {
            _ticks.SpawnsSent += Flush;
            _orders.OrderIssued += OnOrder;
            _orders.AttackIssued += OnAttack;
            _abilities.Activated += OnActivated;
            _abilities.Canceled += OnCanceled;
            _abilities.StateChanged += OnAbilityStateChanged;
        }

        public void LateDispose()
        {
            _ticks.SpawnsSent -= Flush;
            _pendingOrders.Clear();
            _pendingAttacks.Clear();
            _pendingAbilities.Clear();
            _orders.OrderIssued -= OnOrder;
            _orders.AttackIssued -= OnAttack;
            _abilities.Activated -= OnActivated;
            _abilities.Canceled -= OnCanceled;
            _abilities.StateChanged -= OnAbilityStateChanged;
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            _events.MarkBaseline(connection.connectionId);
            foreach (IEntity entity in _entities.Entities)
            {
                if (!entity.TryGetFacade(out IShipAbilityFacade caster)) continue;
                foreach (ShipAbilitySlot slot in caster.Slots)
                    connection.Send(ShipAbilityEventMessage.Capture(slot));
            }
        }

        private void OnAbilityStateChanged(ShipAbilitySlot slot)
        {
            // Active transitions already have their live activation event, with the original target.
            if (_online.HasBegun && slot.State != ShipAbilityState.Active)
                _pendingAbilities.Enqueue((_events.Next(), ShipAbilityEventMessage.Capture(slot)));
        }

        private void OnOrder(UnitOrder order)
        {
            if (_online.HasBegun) _pendingOrders.Enqueue((_events.Next(), new OrderFeedbackMessage(order)));
        }

        private void Flush()
        {
            while (_pendingAttacks.Count > 0)
            {
                var pending = _pendingAttacks.Dequeue();
                Send(pending.Message, pending.Sequence);
            }
            while (_pendingAbilities.Count > 0)
            {
                var pending = _pendingAbilities.Dequeue();
                Send(pending.Message, pending.Sequence);
            }
            while (_pendingOrders.Count > 0)
            {
                var pending = _pendingOrders.Dequeue();
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (connection.connectionId != OnlineLobby.HOST_CONNECTION_ID && _online.IsBattleLoaded(connection) &&
                        _events.IsAfterBaseline(connection.connectionId, pending.Sequence) &&
                        _online.TryGetPlayer(connection.connectionId, out PlayerId player) && player == pending.Message.Issuer)
                        connection.Send(pending.Message);
                }
            }
        }

        private void Send<T>(T message, long sequence) where T : struct, NetworkMessage
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                if (_online.IsBattleLoaded(connection) && _events.IsAfterBaseline(connection.connectionId, sequence))
                    connection.Send(message);
        }

        private void OnAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId)
        {
            if (!_online.HasBegun) return;
            long[] ids = new long[receivers.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = receivers[i].Id;
            _pendingAttacks.Enqueue((_events.Next(), new AttackTargetMessage { ReceiverIds = ids, TargetId = target.Id, HardPointId = hardPointId }));
        }

        private void OnActivated(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!_online.HasBegun) return;
            _pendingAbilities.Enqueue((_events.Next(), new ShipAbilityEventMessage
            {
                CasterId = caster.Entity.Id,
                Ability = id,
                TargetId = target == null ? NO_TARGET : target.Id
            }));
        }

        private void OnCanceled(IShipAbilityFacade caster, ShipAbilityId id)
        {
            if (!_online.HasBegun) return;
            _pendingAbilities.Enqueue((_events.Next(), new ShipAbilityEventMessage { CasterId = caster.Entity.Id, Ability = id, Canceled = true }));
        }
    }
}
