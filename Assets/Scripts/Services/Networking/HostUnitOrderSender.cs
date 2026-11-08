using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: relays each unit's current order once per frame when it changed, so clients show the host's order
    /// and answer new ones with voice lines. Current orders go into each client's baseline.
    /// </summary>
    public sealed class HostUnitOrderSender : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly Dictionary<long, (IUnitOrderReplicaFacade Orders, UnitOrderType Order, int Revision)> _sent =
            new Dictionary<long, (IUnitOrderReplicaFacade, UnitOrderType, int)>();
        private readonly List<long> _changed = new List<long>();

        public HostUnitOrderSender(IEntityLocator entities, OnlineMatchService online, NetworkTickService ticks)
        {
            _entities = entities;
            _online = online;
            _ticks = ticks;
        }

        public void Initialize()
        {
            _ticks.SpawnsSent += Flush;
            foreach (IEntity entity in _entities.Entities) Track(entity);
            _entities.EntityAdded += Track;
            _entities.EntityRemoved += Untrack;
        }

        public void LateDispose()
        {
            _ticks.SpawnsSent -= Flush;
            _entities.EntityAdded -= Track;
            _entities.EntityRemoved -= Untrack;
            _sent.Clear();
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            foreach (KeyValuePair<long, (IUnitOrderReplicaFacade Orders, UnitOrderType Order, int Revision)> entry in _sent)
            {
                if (entry.Value.Order != UnitOrderType.None)
                    connection.Send(new UnitOrderStateMessage { EntityId = entry.Key, Order = entry.Value.Order });
            }
        }

        // A new unit is announced by its first change, after its spawn message.
        private void Track(IEntity entity)
        {
            if (!_sent.ContainsKey(entity.Id) && entity.TryGetFacade(out IUnitOrderReplicaFacade orders))
                _sent.Add(entity.Id, (orders, UnitOrderType.None, orders.OrderRevision));
        }

        private void Untrack(IEntity entity) => _sent.Remove(entity.Id);

        private void Flush()
        {
            _changed.Clear();
            foreach (KeyValuePair<long, (IUnitOrderReplicaFacade Orders, UnitOrderType Order, int Revision)> entry in _sent)
            {
                IUnitOrderReplicaFacade orders = entry.Value.Orders;
                if (orders.CurrentOrder != entry.Value.Order || orders.OrderRevision != entry.Value.Revision)
                    _changed.Add(entry.Key);
            }

            foreach (long entityId in _changed)
            {
                var sent = _sent[entityId];
                UnitOrderType order = sent.Orders.CurrentOrder;
                int revision = sent.Orders.OrderRevision;
                if (_online.HasBegun)
                    HostBroadcast.ToClients(_online, new UnitOrderStateMessage
                    {
                        EntityId = entityId, Order = order, IsNewOrder = revision != sent.Revision
                    });
                _sent[entityId] = (sent.Orders, order, revision);
            }
        }
    }
}
