using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.TacticalMarkers;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host → allied clients. Target tracking moves markers every 0.2 s; a move is sent only once it
    /// reaches <see cref="RESEND_DISTANCE"/>, while adds, refreshes and removals are always sent.
    /// </summary>
    public sealed class HostTacticalMarkerSender : IInitializable, ITickable, ILateDisposable
    {
        private const float RESEND_DISTANCE = 25f;
        private readonly TacticalMarkerRegistry _markers;
        private readonly OnlineMatchService _online;
        private readonly INetworkPlayerDirectory _players;
        private readonly IPlayerRelations _relations;
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly List<int> _disconnected = new List<int>();
        private readonly Dictionary<long, (Vector3 Point, long TargetId, float CreatedAt)> _sent =
            new Dictionary<long, (Vector3, long, float)>();

        public HostTacticalMarkerSender(TacticalMarkerRegistry markers, OnlineMatchService online,
            INetworkPlayerDirectory players, IPlayerRelations relations)
        {
            _markers = markers;
            _online = online;
            _players = players;
            _relations = relations;
        }

        public void Initialize()
        {
            _markers.Added += Send;
            _markers.Updated += SendUpdate;
            _markers.Removed += SendRemoved;
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
                    !_players.TryGetPlayer(connection.connectionId, out PlayerId player) ||
                    !_loaded.Add(connection.connectionId)) continue;
                foreach (TacticalMarker marker in _markers.Markers)
                    if (_relations.IsAllied(marker.Sender, player)) connection.Send(Message(marker));
            }
        }

        private void Send(TacticalMarker marker)
        {
            _sent[marker.Id] = (marker.Point, marker.TargetId, marker.CreatedAt);
            HostBroadcast.ToTeam(_online, _players, _relations, marker.Sender, Message(marker));
        }

        private void SendUpdate(TacticalMarker marker)
        {
            if (_sent.TryGetValue(marker.Id, out var sent) && sent.TargetId == marker.TargetId &&
                sent.CreatedAt == marker.CreatedAt &&
                (sent.Point - marker.Point).sqrMagnitude < RESEND_DISTANCE * RESEND_DISTANCE) return;
            Send(marker);
        }

        private void SendRemoved(TacticalMarker marker)
        {
            _sent.Remove(marker.Id);
            HostBroadcast.ToTeam(_online, _players, _relations, marker.Sender, Message(marker));
        }

        private TacticalMarkerMessage Message(TacticalMarker marker) => new TacticalMarkerMessage
        {
            Id = marker.Id, Type = marker.Type, SenderIndex = marker.Sender.Index,
            Point = marker.Point, TargetId = marker.TargetId, State = marker.State,
            Age = Mathf.Max(0, _markers.Time - marker.CreatedAt),
            Remaining = Mathf.Max(0, marker.ExpiresAt - _markers.Time)
        };

        public void LateDispose()
        {
            _markers.Added -= Send;
            _markers.Updated -= SendUpdate;
            _markers.Removed -= SendRemoved;
            _loaded.Clear();
            _sent.Clear();
        }
    }
}
