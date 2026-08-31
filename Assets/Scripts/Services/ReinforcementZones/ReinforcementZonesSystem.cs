using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position);
        bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position);
        int GetOwnedCapturableZoneCount(PlayerType playerType);
        bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position);
        bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position);
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

        [SerializeField, Min(0f)] private float _spawnEdgePadding = 3f;
        [SerializeField] private ReinforcementZoneView[] _zoneViews = Array.Empty<ReinforcementZoneView>();

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private readonly Dictionary<ShipType, float> _shipNavigationRadii =
            new Dictionary<ShipType, float>();
        private IShipService _shipService;
        private ReinforcementZoneData _data;
        private IRepository _repository;
        private ShipsData _shipsData;

        public event Action OwnershipChanged;

        [Inject]
        private void Construct(
            IShipService shipService,
            ReinforcementZoneData data,
            IRepository repository,
            ShipsData shipsData)
        {
            _shipService = shipService ??
                throw new ArgumentNullException(nameof(shipService));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _repository = repository ??
                throw new ArgumentNullException(nameof(repository));
            _shipsData = shipsData ??
                throw new ArgumentNullException(nameof(shipsData));
        }

        public void Initialize()
        {
            _zones.Clear();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                if (view == null)
                {
                    Debug.LogError("ReinforcementZonesSystem has an unassigned zone view.", this);
                    continue;
                }

                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    view.StartingOwner,
                    view.IsCapturable,
                    view.CaptureDuration,
                    _data.CaptureSpeedPerNetShip);
                _zones.Add(new ReinforcementZonePresenter(model, view));
            }

            if (_zones.Count == 0)
            {
                Debug.LogError("ReinforcementZonesSystem requires at least one explicitly assigned zone view.", this);
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

        public bool IsPositionInAnyZone(Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Contains(position))
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

        public bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position)
        {
            float navigationRadius = GetNavigationRadius(shipType);
            FormationPoint candidate = new FormationPoint(position.x, position.z);
            foreach (IShipEntity ship in _shipService.Ships)
            {
                FormationPoint existing = new FormationPoint(
                    ship.WorldPosition.x,
                    ship.WorldPosition.z);
                if (!FormationModel.HasClearance(
                        candidate,
                        navigationRadius,
                        existing,
                        ship.NavigationRadius))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetRandomSpawnPosition(
            PlayerType playerType,
            ShipType shipType,
            out Vector3 position)
        {
            List<ReinforcementZonePresenter> ownedZones = new List<ReinforcementZonePresenter>();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner == playerType)
                {
                    ownedZones.Add(zone);
                }
            }

            if (ownedZones.Count == 0)
            {
                position = default;
                return false;
            }

            float navigationRadius = GetNavigationRadius(shipType);
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                ReinforcementZonePresenter selectedZone =
                    ownedZones[Random.Range(0, ownedZones.Count)];
                float radius = Mathf.Max(
                    0f,
                    selectedZone.Radius - _spawnEdgePadding - navigationRadius);
                Vector2 offset = Random.insideUnitCircle * radius;
                position = selectedZone.Center + new Vector3(offset.x, 0f, offset.y);
                position.y = 0f;
                if (IsShipSpawnPositionClear(shipType, position))
                {
                    return true;
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
    }
}
