using System;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>
    /// Online: the route line a ship shows while selected. The host relays changes; clients move ships from
    /// replication and never plan routes, so they show the host's route instead. Not used by gameplay.
    /// </summary>
    public interface IShipRouteReplicaFacade : IEntityFacade
    {
        /// <summary>Host: the shown route changed; empty when cleared.</summary>
        event Action<Vector3[]> RouteChanged;

        Vector3[] Route { get; }

        /// <summary>Client: shows the host's route; empty clears it.</summary>
        void ShowReplicaRoute(Vector3[] route);
    }
}
