using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: relays each ship's route line to its owner's client. Only the newest route per ship is sent each
    /// frame, since pursuit re-plans several times a second. Current routes go into each client's baseline.
    /// </summary>
    public sealed class HostShipRouteSender : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly NetworkEventSequence _events = new NetworkEventSequence();
        private readonly Dictionary<long, (IEntity Entity, IShipRouteReplicaFacade Route, Action<Vector3[]> Handler)> _tracked =
            new Dictionary<long, (IEntity, IShipRouteReplicaFacade, Action<Vector3[]>)>();
        private readonly Dictionary<long, (long Sequence, IEntity Entity, ShipRouteMessage Message)> _pending =
            new Dictionary<long, (long, IEntity, ShipRouteMessage)>();

        public HostShipRouteSender(IEntityLocator entities, OnlineMatchService online, NetworkTickService ticks)
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
            foreach (var tracked in _tracked.Values) tracked.Route.RouteChanged -= tracked.Handler;
            _tracked.Clear();
            _pending.Clear();
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            _events.MarkBaseline(connection.connectionId);
            if (!_online.TryGetPlayer(connection.connectionId, out PlayerId player)) return;
            foreach (var tracked in _tracked.Values)
            {
                if (tracked.Entity.Owner == player && tracked.Route.Route.Length > 1)
                    connection.Send(new ShipRouteMessage { EntityId = tracked.Entity.Id, Route = tracked.Route.Route });
            }
        }

        private void Track(IEntity entity)
        {
            if (_tracked.ContainsKey(entity.Id) || !entity.TryGetFacade(out IShipRouteReplicaFacade route)) return;
            Action<Vector3[]> handler = waypoints => Enqueue(entity, waypoints);
            route.RouteChanged += handler;
            _tracked.Add(entity.Id, (entity, route, handler));
        }

        private void Untrack(IEntity entity)
        {
            if (!_tracked.TryGetValue(entity.Id, out var tracked)) return;
            tracked.Route.RouteChanged -= tracked.Handler;
            _tracked.Remove(entity.Id);
            _pending.Remove(entity.Id);
        }

        private void Enqueue(IEntity entity, Vector3[] route)
        {
            if (!_online.HasBegun) return;
            _pending[entity.Id] = (_events.Next(), entity,
                new ShipRouteMessage { EntityId = entity.Id, Route = route });
        }

        private void Flush()
        {
            foreach (var pending in _pending.Values)
            {
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (connection.connectionId != OnlineLobby.HOST_CONNECTION_ID && _online.IsBattleLoaded(connection) &&
                        _events.IsAfterBaseline(connection.connectionId, pending.Sequence) &&
                        _online.TryGetPlayer(connection.connectionId, out PlayerId player) && player == pending.Entity.Owner)
                        connection.Send(pending.Message);
                }
            }

            _pending.Clear();
        }
    }
}
