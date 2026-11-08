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
        private readonly Dictionary<long, WeaponTargetRecord> _records = new Dictionary<long, WeaponTargetRecord>();
        private readonly List<WeaponTargetMessage> _baseline = new List<WeaponTargetMessage>();

        public HostWeaponTargetSender(IEntityLocator entities, OnlineMatchService online)
        {
            _entities = entities;
            _online = online;
        }

        public void Initialize()
        {
            foreach (IEntity entity in _entities.Entities) Track(entity);
            _entities.EntityAdded += Track;
            _entities.EntityRemoved += Untrack;
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= Track;
            _entities.EntityRemoved -= Untrack;
            foreach (WeaponTargetRecord record in _records.Values) record.Dispose();
            _records.Clear();
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
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
            if (_online.HasBegun) HostBroadcast.ToClients(_online, message);
        }
    }
}
