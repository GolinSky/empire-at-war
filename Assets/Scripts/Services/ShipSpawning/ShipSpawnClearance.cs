using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SpawnArea;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.StationFacing;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class ShipSpawnClearance : IShipSpawnClearance
    {
        // Matches ShipInstaller's prefab path: "<ShipType>" + "Ship" + "View".
        private const string SHIP_VIEW_SUFFIX = "ShipView";

        private readonly IAssetService _assetService;
        private readonly IStationFacingService _stationFacingService;

        private readonly ShipsData _shipsData;
        private readonly Dictionary<ShipType, Hull> _hulls = new Dictionary<ShipType, Hull>();
        private readonly Dictionary<object, ShipHullFootprint> _landings =
            new Dictionary<object, ShipHullFootprint>();
        private Collider[] _obstacles = new Collider[32];

        public ShipSpawnClearance(
            IAssetService assetService,
            IStationFacingService stationFacingService,
            ShipsData shipsData)
        {
            _assetService = assetService;
            _stationFacingService = stationFacingService;
            _shipsData = shipsData;
        }

        public float GetPlanarRadius(ShipType shipType)
        {
            Hull hull = GetHull(shipType);
            return new Vector2(
                Mathf.Abs(hull.LocalCenter.x) + hull.HalfExtents.x,
                Mathf.Abs(hull.LocalCenter.z) + hull.HalfExtents.z).magnitude;
        }

        public Bounds GetLocalHullBounds(ShipType shipType) => GetHull(shipType).LocalBounds;

        public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position)
        {
            ShipHullFootprint footprint = GetFootprint(owner, shipType, position);
            if (Physics.CheckBox(footprint.Center, footprint.HalfExtents, footprint.Rotation,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Ships in hyperspace are still far from their landing point, so physics cannot see them.
            foreach (ShipHullFootprint landing in _landings.Values)
            {
                if (landing.Overlaps(footprint))
                {
                    return false;
                }
            }

            return true;
        }

        // Broad phase only: the placement predicate remains the source of truth at each candidate cell.
        public void BlockObstructedCells(PlayerId owner, ShipType shipType, SpawnAreaGridModel grid,
            Bounds map, Func<float, float, bool> isBlocked)
        {
            Bounds hull = GetFootprint(owner, shipType, Vector3.zero).Bounds;
            Bounds query = new Bounds(
                new Vector3(map.center.x + hull.center.x, hull.center.y, map.center.z + hull.center.z),
                new Vector3(map.size.x + hull.size.x, hull.size.y, map.size.z + hull.size.z));
            int count;
            while ((count = Physics.OverlapBoxNonAlloc(query.center, query.extents, _obstacles,
                       Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) ==
                   _obstacles.Length)
                Array.Resize(ref _obstacles, _obstacles.Length * 2);

            for (int i = 0; i < count; i++)
                BlockCandidate(_obstacles[i].bounds, hull, grid, isBlocked);

            foreach (ShipHullFootprint landing in _landings.Values)
                BlockCandidate(landing.Bounds, hull, grid, isBlocked);
        }

        private static void BlockCandidate(Bounds obstacle, Bounds hull, SpawnAreaGridModel grid,
            Func<float, float, bool> isBlocked)
        {
            if (obstacle.max.y < hull.min.y || obstacle.min.y > hull.max.y) return;
            grid.BlockWhere(
                obstacle.min.x - hull.max.x, obstacle.min.z - hull.max.z,
                obstacle.max.x - hull.min.x, obstacle.max.z - hull.min.z, isBlocked);
        }

        public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position)
        {
            _landings.Add(ship, GetFootprint(owner, shipType, position));
        }

        public void ReleaseLanding(object ship)
        {
            _landings.Remove(ship);
        }

        private ShipHullFootprint GetFootprint(PlayerId owner, ShipType shipType, Vector3 position)
        {
            Hull hull = GetHull(shipType);
            Quaternion rotation = _stationFacingService.GetRotation(owner);
            position.y = hull.Height;
            return new ShipHullFootprint(position + rotation * hull.LocalCenter, hull.HalfExtents, rotation);
        }

        private Hull GetHull(ShipType shipType)
        {
            if (_hulls.TryGetValue(shipType, out Hull hull))
            {
                return hull;
            }

            BoxCollider collider = _assetService.LoadComponent<BoxCollider>(shipType + SHIP_VIEW_SUFFIX);
            Vector3 scale = collider.transform.lossyScale;
            ShipData shipData = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            hull = new Hull(new Bounds(collider.center, collider.size), scale, shipData.Height);
            _hulls.Add(shipType, hull);
            return hull;
        }

        private readonly struct Hull
        {
            public Hull(Bounds localBounds, Vector3 scale, float height)
            {
                LocalBounds = localBounds;
                LocalCenter = Vector3.Scale(localBounds.center, scale);
                HalfExtents = Vector3.Scale(localBounds.extents, scale);
                Height = height;
            }

            public Bounds LocalBounds { get; }
            public Vector3 LocalCenter { get; }
            public Vector3 HalfExtents { get; }
            public float Height { get; }
        }
    }
}
