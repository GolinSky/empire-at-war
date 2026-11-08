using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: relays every weapon target change (AI, radar, orders) so client turrets engage what the host's do.
    /// Current targets go into each client's baseline (<see cref="SendBaseline"/>).
    /// </summary>
    public sealed class HostWeaponTargetSender : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly NetworkEventSequence _events = new NetworkEventSequence();
        private readonly Queue<(long Sequence, WeaponTargetMessage Message)> _pending = new Queue<(long, WeaponTargetMessage)>();
        private readonly Dictionary<long, WeaponTargetRecord> _records = new Dictionary<long, WeaponTargetRecord>();
        private readonly List<WeaponTargetMessage> _baseline = new List<WeaponTargetMessage>();

        public HostWeaponTargetSender(IEntityLocator entities, OnlineMatchService online, NetworkTickService ticks)
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
            _pending.Clear();
            _entities.EntityAdded -= Track;
            _entities.EntityRemoved -= Untrack;
            foreach (WeaponTargetRecord record in _records.Values) record.Dispose();
            _records.Clear();
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            _events.MarkBaseline(connection.connectionId);
            _baseline.Clear();
            foreach (WeaponTargetRecord record in _records.Values) record.CollectBaseline(_baseline);
            foreach (WeaponTargetMessage message in _baseline) connection.Send(message);
        }

        private void Track(IEntity entity)
        {
            if (_records.ContainsKey(entity.Id) ||
                !entity.TryGetFacade(out IWeaponTargetReplicaFacade weapon)) return;
            _records.Add(entity.Id, new WeaponTargetRecord(entity.Id, weapon, _entities, Send));
        }

        private void Untrack(IEntity entity)
        {
            if (!_records.TryGetValue(entity.Id, out WeaponTargetRecord record)) return;
            record.Dispose();
            _records.Remove(entity.Id);
        }

        private void Send(WeaponTargetMessage message)
        {
            if (_online.HasBegun) _pending.Enqueue((_events.Next(), message));
        }

        private void Flush()
        {
            while (_pending.Count > 0)
            {
                var pending = _pending.Dequeue();
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                    if (_online.IsBattleLoaded(connection) && _events.IsAfterBaseline(connection.connectionId, pending.Sequence))
                        connection.Send(pending.Message);
            }
        }
    }
}
