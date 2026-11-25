using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.UnitOrders;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host → clients: focus fire holder counts for the client's own team. Targets changed during a frame are sent
    /// once at the next tick; a client that finishes loading gets every held target.
    /// </summary>
    public sealed class HostFocusFireSender : IInitializable, ITickable, ILateDisposable
    {
        private readonly IFocusFireSlots _slots;
        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly INetworkPlayerDirectory _players;
        private readonly HashSet<long> _dirty = new HashSet<long>();
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly List<int> _disconnected = new List<int>();
        private readonly List<IEntity> _held = new List<IEntity>();

        public HostFocusFireSender(IFocusFireSlots slots, IEntityLocator entities, OnlineMatchService online,
            INetworkPlayerDirectory players)
        {
            _slots = slots;
            _entities = entities;
            _online = online;
            _players = players;
        }

        public void Initialize() => _slots.Changed += MarkDirty;

        public void LateDispose()
        {
            _slots.Changed -= MarkDirty;
            _dirty.Clear();
            _loaded.Clear();
        }

        public void Tick()
        {
            _disconnected.Clear();
            foreach (int id in _loaded)
                if (!NetworkServer.connections.ContainsKey(id)) _disconnected.Add(id);
            foreach (int id in _disconnected) _loaded.Remove(id);

            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!_online.IsBattleLoaded(connection) ||
                    !_players.TryGetPlayer(connection.connectionId, out PlayerId player)) continue;
                if (_loaded.Add(connection.connectionId))
                {
                    _slots.CopyHeldTargets(_held);
                    foreach (IEntity target in _held) connection.Send(Message(target, player));
                    continue;
                }

                foreach (long targetId in _dirty)
                    if (_entities.TryGetEntity(targetId, out IEntity target))
                        connection.Send(Message(target, player));
            }

            _dirty.Clear();
        }

        private void MarkDirty(IEntity target) => _dirty.Add(target.Id);

        private FocusFireCountMessage Message(IEntity target, PlayerId player) => new FocusFireCountMessage
        {
            TargetId = target.Id,
            Ships = _slots.CountHolders(player, target, isSquadron: false),
            Squadrons = _slots.CountHolders(player, target, isSquadron: true)
        };
    }
}
