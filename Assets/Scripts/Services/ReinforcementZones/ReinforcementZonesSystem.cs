using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position, float clearance = 0f);
        void CopyOwnedCapturableZoneCenters(PlayerType playerType, List<Vector3> destination);
        bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position);
        int GetOwnedCapturableZoneCount(PlayerType playerType);
        bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position);
        bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position);
        bool TryGetDefaultZoneCenter(PlayerType playerType, out Vector3 position);
        bool TryGetDefaultZoneExitPosition(
            PlayerType playerType,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position);
        bool TryGetRandomSpawnPosition(
            PlayerType playerType,
            ShipType shipType,
            out Vector3 position);
        bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position);
    }

    public sealed class ReinforcementZonesSystem : MonoBehaviour, IReinforcementZonesSystem, IInitializable, ITickable
    {
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const int MAX_LAYOUT_ATTEMPTS = 72;
        private const float ZONE_CLEARANCE = 30f;
        private const float CAPTURABLE_ZONE_SPACING = 150f;

        // XZ footprint radii about each station's pivot, including its model offset.
        [SerializeField, Min(0f)] private float republicStationRadius = 135f;
        [SerializeField, Min(0f)] private float separatistStationRadius = 90f;
        [SerializeField, Min(0f)] private float _spawnEdgePadding = 3f;
        [SerializeField] private ReinforcementZoneView[] _zoneViews = Array.Empty<ReinforcementZoneView>();

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private readonly Dictionary<ShipType, float> _shipNavigationRadii =
            new Dictionary<ShipType, float>();
        private IShipService _shipService;
        private ReinforcementZoneData _data;
        private IMapModelObserver _mapModel;
        private IShipNavigationService _shipNavigationService;
        private IAssetService _repository;
        private ShipsData _shipsData;
        private FactionType _playerFactionType;
        private FactionType _opponentFactionType;

        public event Action OwnershipChanged;
        public IReadOnlyList<ReinforcementZonePresenter> Zones => _zones;

        [Inject]
        private void Construct(
            IShipService shipService,
            ReinforcementZoneData data,
            IAssetService repository,
            ShipsData shipsData,
            IMapModelObserver mapModel,
            IShipNavigationService shipNavigationService,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType,
            [Inject(Id = PlayerType.Opponent)] FactionType opponentFactionType)
        {
            _shipService = shipService;
            _data = data;
            _repository = repository;
            _shipsData = shipsData;
            _mapModel = mapModel;
            _shipNavigationService = shipNavigationService;
            _playerFactionType = playerFactionType;
            _opponentFactionType = opponentFactionType;
        }

        public void Initialize()
        {
            _zones.Clear();
            ArrangeZones();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    view.StartingOwner,
                    view.IsCapturable,
                    view.CaptureDuration,
                    _data.CaptureSpeedPerNetShip);
                _zones.Add(new ReinforcementZonePresenter(model, view));
            }
        }

        public void Tick()
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                int playerShips = 0;
                int opponentShips = 0;

                foreach (IShipEntity ship in _shipService.Ships)
                {
                    if (!zone.Contains(ship.WorldPosition))
                    {
                        continue;
                    }

                    if (ship.PlayerType == PlayerType.Player)
                    {
                        playerShips++;
                    }
                    else if (ship.PlayerType == PlayerType.Opponent)
                    {
                        opponentShips++;
                    }
                }

                if (zone.Tick(Time.deltaTime, playerShips, opponentShips))
                {
                    OwnershipChanged?.Invoke();
                }
            }
        }

        public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner == playerType && zone.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPositionInAnyZone(Vector3 position, float clearance = 0f)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                float x = position.x - zone.Center.x;
                float z = position.z - zone.Center.z;
                float radius = zone.Radius + clearance;
                if (x * x + z * z <= radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetOwnedCapturableZoneCount(PlayerType playerType)
        {
            int count = 0;
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.IsCapturable && zone.Owner == playerType)
                {
                    count++;
                }
            }

            return count;
        }

        public void CopyOwnedCapturableZoneCenters(PlayerType playerType, List<Vector3> destination)
        {
            destination.Clear();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.IsCapturable && zone.Owner == playerType)
                {
                    destination.Add(zone.Center);
                }
            }
        }

        public bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position)
        {
            float navigationRadius = GetNavigationRadius(shipType);
            return _shipNavigationService.IsPositionClear(
                position,
                navigationRadius);
        }

        public bool TryGetRandomSpawnPosition(
            PlayerType playerType,
            ShipType shipType,
            out Vector3 position)
        {
            List<ReinforcementZonePresenter> ownedZones = new List<ReinforcementZonePresenter>();
            List<ReinforcementZonePresenter> capturedZones = new List<ReinforcementZonePresenter>();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner != playerType)
                {
                    continue;
                }

                ownedZones.Add(zone);
                if (zone.IsCapturable && playerType == PlayerType.Opponent)
                {
                    capturedZones.Add(zone);
                }
            }

            if (capturedZones.Count > 0 &&
                TryGetClearSpawnPosition(capturedZones, shipType, out position))
            {
                return true;
            }

            return TryGetClearSpawnPosition(ownedZones, shipType, out position);
        }

        private bool TryGetClearSpawnPosition(
            IReadOnlyList<ReinforcementZonePresenter> zones,
            ShipType shipType,
            out Vector3 position)
        {
            if (zones.Count > 0)
            {
                float navigationRadius = GetNavigationRadius(shipType);
                for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
                {
                    ReinforcementZonePresenter selectedZone =
                        zones[Random.Range(0, zones.Count)];
                    float radius = selectedZone.Radius - _spawnEdgePadding - navigationRadius;
                    if (radius < 0f)
                    {
                        continue;
                    }

                    Vector2 offset = Random.insideUnitCircle * radius;
                    position = selectedZone.Center + new Vector3(offset.x, 0f, offset.y);
                    position.y = 0f;
                    if (IsShipSpawnPositionClear(shipType, position))
                    {
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        public bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner != playerType)
                {
                    continue;
                }

                float radius = Mathf.Max(0f, zone.Radius - _spawnEdgePadding);
                Vector2 offset = Random.insideUnitCircle * radius;
                position = zone.Center + new Vector3(offset.x, 0f, offset.y);
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetDefaultZoneCenter(PlayerType playerType, out Vector3 position)
        {
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                if (view.StartingOwner != playerType || view.IsCapturable) continue;
                position = view.Center;
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetDefaultZoneExitPosition(
            PlayerType playerType,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner != playerType || zone.IsCapturable ||
                    !zone.Contains(shipPosition))
                {
                    continue;
                }

                Vector3 direction = new Vector3(
                    (_mapModel.SizeRange.Min.x + _mapModel.SizeRange.Max.x) * 0.5f - zone.Center.x,
                    0f,
                    (_mapModel.SizeRange.Min.y + _mapModel.SizeRange.Max.y) * 0.5f - zone.Center.z);
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Vector3.right;
                }

                position = zone.Center + direction.normalized *
                    (zone.Radius + shipRadius + _spawnEdgePadding);
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
        {
            ReinforcementZonePresenter closestZone = null;
            float closestDistance = float.MaxValue;

            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (!zone.IsCapturable || zone.Owner == playerType)
                {
                    continue;
                }

                float distance = (zone.Center - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }

            if (closestZone == null)
            {
                position = default;
                return false;
            }

            position = closestZone.Center;
            position.y = 0f;
            return true;
        }

        private float GetNavigationRadius(ShipType shipType)
        {
            if (_shipNavigationRadii.TryGetValue(
                    shipType,
                    out float navigationRadius))
            {
                return navigationRadius;
            }

            string dataPath = _shipsData.GetShipDataPath(shipType);
            ShipData shipData = _repository.Load<ShipData>(dataPath);
            if (shipData == null)
            {
                throw new InvalidOperationException(
                    $"Ship data for {shipType} could not be loaded.");
            }

            navigationRadius = Mathf.Max(
                shipData.NavigationRadius,
                MINIMUM_NAVIGATION_RADIUS);
            _shipNavigationRadii.Add(shipType, navigationRadius);
            return navigationRadius;
        }

        private void ArrangeZones()
        {
            List<ReinforcementZoneView> placed = new List<ReinforcementZoneView>();
            List<ReinforcementZoneView> capturable = new List<ReinforcementZoneView>();
            Vector2 mapCenter = (_mapModel.SizeRange.Min + _mapModel.SizeRange.Max) * 0.5f;
            float largestRadius = 0f;
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                if (view.IsCapturable)
                {
                    capturable.Add(view);
                    largestRadius = Mathf.Max(largestRadius, view.Radius);
                    continue;
                }

                FactionType faction = view.StartingOwner == PlayerType.Player
                    ? _playerFactionType
                    : _opponentFactionType;
                Vector3 station = _mapModel.GetStationPosition(faction);
                float stationRadius = faction == FactionType.Republic
                    ? republicStationRadius
                    : separatistStationRadius;
                float side = station.x < mapCenter.x ? 1f : -1f;
                Vector3 center = new Vector3(
                    station.x + side * (stationRadius + view.Radius + ZONE_CLEARANCE),
                    view.Center.y,
                    station.z);
                if (!IsZonePositionClear(center, view.Radius, placed))
                {
                    throw new InvalidOperationException(
                        $"No room beside the {faction} station for its default reinforcement zone.");
                }

                view.SetCenter(center);
                placed.Add(view);
            }

            if (capturable.Count == 0)
            {
                return;
            }

            ReinforcementZoneView centralZone = capturable[0];
            Vector3 centralPosition = new Vector3(mapCenter.x, centralZone.Center.y, mapCenter.y);
            if (!IsZonePositionClear(centralPosition, largestRadius, placed))
            {
                throw new InvalidOperationException("The map center must have room for a capturable zone.");
            }

            centralZone.SetCenter(centralPosition);
            placed.Add(centralZone);
            if (capturable.Count == 1)
            {
                return;
            }

            float spacing = Mathf.Max(CAPTURABLE_ZONE_SPACING, largestRadius * 2f + ZONE_CLEARANCE);
            Vector2 mapSize = _mapModel.SizeRange.Max - _mapModel.SizeRange.Min;
            int extent = Mathf.CeilToInt(Mathf.Max(mapSize.x, mapSize.y) / spacing);
            List<Vector3> candidates = new List<Vector3>();
            for (int attempt = 0; attempt < MAX_LAYOUT_ATTEMPTS; attempt++)
            {
                candidates.Clear();
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                // A randomly rotated triangular grid keeps uniform neighbor spacing,
                // with the origin reserved for the central capture zone.
                for (int row = -extent; row <= extent; row++)
                {
                    for (int column = -extent; column <= extent; column++)
                    {
                        if (row == 0 && column == 0)
                        {
                            continue;
                        }

                        float x = (column + row * 0.5f) * spacing;
                        float z = row * Mathf.Sqrt(3f) * 0.5f * spacing;
                        Vector3 candidate = new Vector3(
                            mapCenter.x + x * cosine - z * sine,
                            0f,
                            mapCenter.y + x * sine + z * cosine);
                        if (IsZonePositionClear(candidate, largestRadius, placed))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count < capturable.Count - 1)
                {
                    continue;
                }

                candidates.Sort((a, b) =>
                    (a - centralPosition).sqrMagnitude.CompareTo((b - centralPosition).sqrMagnitude));
                for (int i = 1; i < capturable.Count; i++)
                {
                    Vector3 position = candidates[i - 1];
                    position.y = capturable[i].Center.y;
                    capturable[i].SetCenter(position);
                }

                return;
            }

            throw new InvalidOperationException(
                "The map cannot fit its capturable zones at the required uniform spacing.");
        }

        private bool IsZonePositionClear(
            Vector3 center, float radius, IReadOnlyList<ReinforcementZoneView> placed)
        {
            if (center.x - radius < _mapModel.SizeRange.Min.x ||
                center.x + radius > _mapModel.SizeRange.Max.x ||
                center.z - radius < _mapModel.SizeRange.Min.y ||
                center.z + radius > _mapModel.SizeRange.Max.y)
            {
                return false;
            }

            Vector3 republic = _mapModel.GetStationPosition(FactionType.Republic);
            Vector3 separatist = _mapModel.GetStationPosition(FactionType.Separatist);
            float republicClearance = radius + republicStationRadius + ZONE_CLEARANCE;
            float separatistClearance = radius + separatistStationRadius + ZONE_CLEARANCE;
            if (new Vector2(center.x - republic.x, center.z - republic.z).sqrMagnitude <
                    republicClearance * republicClearance ||
                new Vector2(center.x - separatist.x, center.z - separatist.z).sqrMagnitude <
                    separatistClearance * separatistClearance)
            {
                return false;
            }

            foreach (ReinforcementZoneView other in placed)
            {
                float clearance = radius + other.Radius + ZONE_CLEARANCE;
                if (new Vector2(center.x - other.Center.x, center.z - other.Center.z).sqrMagnitude <
                    clearance * clearance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
