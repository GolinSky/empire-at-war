using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal interface IShipPathGraph : IDisposable
    {
        bool Matches(IReadOnlyList<RadarContact> obstacles, Vector2Range mapRange, float clearance,
            IReadOnlyList<IHazardSource> hazards);
        IReadOnlyList<NavMeshCorner> RouteCorners { get; }
        NativeArray<float3> Disks { get; }
        NativeParallelMultiHashMap<int, int> DiskBuckets { get; }
        NavMeshBuckets Layout { get; }
        void SetOrigin(Vector3 origin);
        bool IsUnpenalizedSegment(Vector3 from, Vector3 to);
        bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints);
        bool IsKnownUnreachable(Vector3 position);
        bool IsReachable(Vector3 position);
        bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest);
    }
}
