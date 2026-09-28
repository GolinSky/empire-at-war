using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using ViewComponents;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position, float clearance = 0f);
        void CopyOwnedCapturableZoneCenters(PlayerId owner, List<Vector3> destination);
        bool IsPositionInAlliedZone(PlayerId owner, Vector3 position);
        int GetOwnedCapturableZoneCount(PlayerId owner);
        bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position);
        bool TryGetDefaultSpawnPosition(PlayerId owner, out Vector3 position);
        bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position);
        bool TryGetDefaultZoneExitPosition(
            PlayerId owner,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position);
        bool TryGetRandomSpawnPosition(
            PlayerId owner,
            ShipType shipType,
            out Vector3 position);
        bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position);
    }

    public sealed class ReinforcementZonesSystem : MonoBehaviour, IReinforcementZonesSystem, IInitializable, ITickable
    {
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float MINIMUM_ZONE_VISIBILITY = 0.5f;

        [SerializeField, Min(0f)] private float _spawnEdgePadding = 3f;

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private readonly Dictionary<ShipType, float> _shipNavigationRadii =
            new Dictionary<ShipType, float>();
        private readonly List<IEntity> _squadrons = new List<IEntity>();
        private IShipService _shipService;
        private IEntityLocator _entityLocator;
        private FogOfWarSystem _fogOfWarSystem;
        private ICameraService _cameraService;
        private IInputService _inputService;
        private ReinforcementZoneData _data;
        private IMapModelObserver _mapModel;
        private IShipNavigationService _shipNavigationService;
        private IAssetService _repository;
        private ShipsData _shipsData;
        private ReinforcementZoneView[] _zoneViews;
        private IPlayerRoster _roster;
        private ILocalPlayer _localPlayer;
        private CaptureTallyBuilder _tally;

        public event Action OwnershipChanged;
        public IReadOnlyList<ReinforcementZonePresenter> Zones => _zones;

        [Inject]
        private void Construct(
            IShipService shipService,
            IEntityLocator entityLocator,
            ReinforcementZoneData data,
            IAssetService repository,
            ShipsData shipsData,
            IMapModelObserver mapModel,
            IShipNavigationService shipNavigationService,
            FogOfWarSystem fogOfWarSystem,
            ICameraService cameraService,
            IInputService inputService,
            ReinforcementZoneView[] zoneViews,
            IPlayerRoster roster,
            ILocalPlayer localPlayer)
        {
            _shipService = shipService;
            _entityLocator = entityLocator;
            _data = data;
            _repository = repository;
            _shipsData = shipsData;
            _mapModel = mapModel;
            _shipNavigationService = shipNavigationService;
            _fogOfWarSystem = fogOfWarSystem;
            _cameraService = cameraService;
            _inputService = inputService;
            _zoneViews = zoneViews;
            _roster = roster;
            _localPlayer = localPlayer;
            _tally = new CaptureTallyBuilder(roster);
        }

        public void Initialize()
        {
            _zones.Clear();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    view.StartingOwner,
                    view.IsCapturable,
                    view.CaptureDuration,
                    _data.CaptureSpeedPerNetShip,
                    _roster);
                _zones.Add(new ReinforcementZonePresenter(model, view, _localPlayer));
            }
        }

        public void Tick()
        {
            CollectSquadrons();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                _tally.Clear();
                _shipService.AddShipStrength(zone.Contains, _tally);

                // A squadron is positioned at the centroid of its fighters.
                foreach (IEntity squadron in _squadrons)
                {
                    if (zone.Contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position))
                    {
                        _tally.Add(squadron.Owner, _data.SquadronCaptureWeight);
                    }
                }

                if (zone.Tick(Time.deltaTime, _tally.Build()))
                {
                    OwnershipChanged?.Invoke();
                }

                // The circle stays visible; labels and minimap markers require current vision.
                bool isRevealed = !_fogOfWarSystem.IsHidden(zone.Center, MINIMUM_ZONE_VISIBILITY);
                bool isHovered = isRevealed && _inputService.SupportsHover &&
                    zone.Contains(_cameraService.GetWorldPoint(_inputService.TouchPosition, zone.Center));
                zone.SetVisibility(isRevealed, isHovered);
            }
        }

        private void CollectSquadrons()
        {
            _squadrons.Clear();
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Model is ISquadronModelObserver && !entity.HealthModel.IsDestroyed)
                {
                    _squadrons.Add(entity);
                }
            }
        }

        public bool IsPositionInAlliedZone(PlayerId owner, Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (_roster.IsAllied(zone.Owner, owner) && zone.Contains(position))
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

        public int GetOwnedCapturableZoneCount(PlayerId owner)
        {
            int count = 0;
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.IsCapturable && zone.Owner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        public void CopyOwnedCapturableZoneCenters(PlayerId owner, List<Vector3> destination)
        {
            destination.Clear();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.IsCapturable && zone.Owner == owner)
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
            PlayerId owner,
            ShipType shipType,
            out Vector3 position)
        {
            // Reinforcements may arrive in any zone held by the owner's team.
            List<ReinforcementZonePresenter> alliedZones = new List<ReinforcementZonePresenter>();
            List<ReinforcementZonePresenter> capturedZones = new List<ReinforcementZonePresenter>();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (!_roster.IsAllied(zone.Owner, owner))
                {
                    continue;
                }

                alliedZones.Add(zone);
                // AI players reinforce at their team's captured front-line zones first.
                if (zone.IsCapturable && _roster.Get(owner).IsAi)
                {
                    capturedZones.Add(zone);
                }
            }

            if (capturedZones.Count > 0 &&
                TryGetClearSpawnPosition(capturedZones, shipType, out position))
            {
                return true;
            }

            return TryGetClearSpawnPosition(alliedZones, shipType, out position);
        }

        private bool TryGetClearSpawnPosition(
            IReadOnlyList<ReinforcementZonePresenter> zones,
            ShipType shipType,
            out Vector3 position)
        {
            // Prefer the whole hull inside the zone. When the zone is crowded that leaves capital ships
            // almost no room, so fall back to the player placement rule: only the centre must be inside.
            return TryGetClearSpawnPosition(zones, shipType, true, out position) ||
                   TryGetClearSpawnPosition(zones, shipType, false, out position);
        }

        private bool TryGetClearSpawnPosition(
            IReadOnlyList<ReinforcementZonePresenter> zones,
            ShipType shipType,
            bool keepHullInside,
            out Vector3 position)
        {
            if (zones.Count > 0)
            {
                float navigationRadius = GetNavigationRadius(shipType);
                for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
                {
                    ReinforcementZonePresenter selectedZone =
                        zones[Random.Range(0, zones.Count)];
                    float hullRadius = selectedZone.Radius - _spawnEdgePadding - navigationRadius;
                    if (hullRadius < 0f)
                    {
                        continue;
                    }

                    float radius = keepHullInside ? hullRadius : selectedZone.Radius - _spawnEdgePadding;
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

        public bool TryGetDefaultSpawnPosition(PlayerId owner, out Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner != owner)
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

        public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position)
        {
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                if (view.StartingOwner != owner || view.IsCapturable) continue;
                position = view.Center;
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetDefaultZoneExitPosition(
            PlayerId owner,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner != owner || zone.IsCapturable ||
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

        public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
        {
            ReinforcementZonePresenter closestZone = null;
            float closestDistance = float.MaxValue;

            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (!zone.IsCapturable || zone.Owner == owner)
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
    }
}
