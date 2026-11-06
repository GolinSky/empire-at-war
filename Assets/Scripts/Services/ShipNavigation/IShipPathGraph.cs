using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal interface IShipPathGraph : IDisposable
    {
        bool Matches(IReadOnlyList<RadarContact> obstacles, Vector2Range mapRange, float clearance,
            IReadOnlyList<IHazardSource> hazards);
        void SetOrigin(Vector3 origin);
        bool ContainsRoute(ShipBezierRoute route);
        bool IsUnpenalizedSegment(Vector3 from, Vector3 to);
        bool PreservesTravelCost(ShipBezierRoute route, IReadOnlyList<Vector3> waypoints);
        bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints);
        bool IsKnownUnreachable(Vector3 position);
        bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest);
    }
}
