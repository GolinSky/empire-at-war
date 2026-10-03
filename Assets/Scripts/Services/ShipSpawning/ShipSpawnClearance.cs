using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
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
            hull = new Hull(
                Vector3.Scale(collider.center, scale),
                Vector3.Scale(collider.size, scale) * 0.5f,
                shipData.Height);
            _hulls.Add(shipType, hull);
            return hull;
        }

        private readonly struct Hull
        {
            public Hull(Vector3 localCenter, Vector3 halfExtents, float height)
            {
                LocalCenter = localCenter;
                HalfExtents = halfExtents;
                Height = height;
            }

            public Vector3 LocalCenter { get; }
            public Vector3 HalfExtents { get; }
            public float Height { get; }
        }
    }
}
