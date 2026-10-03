using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position, float clearance = 0f);

        void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination);

        bool IsPositionInAlliedZone(PlayerId owner, Vector3 position);

        int GetOwnedCapturableZoneCount(PlayerId owner);

        bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position);

        bool TryGetDefaultZoneExitPosition(
            PlayerId owner,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position);

        bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position);
    }

    public sealed class ReinforcementZonesSystem : MonoBehaviour, IReinforcementZonesSystem, IReinforcementZoneSource,
        IInitializable, ITickable
    {
        private const float MINIMUM_ZONE_VISIBILITY = 0.5f;

        private IShipService _shipService;
        private ISquadronRegistry _squadronRegistry;
        private IFogOfWarSystem _fogOfWarSystem;
        private ICameraService _cameraService;
        private IPointerInput _pointer;
        private IMapModelObserver _mapModel;
        private IPlayerRoster _roster;
        private ILocalPlayer _localPlayer;

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private ReinforcementZoneData _data;
        private ReinforcementZoneView[] _zoneViews;
        private CaptureStrengthBuilder _tally;

        [SerializeField, Min(0f)] private float _spawnEdgePadding = 3f;

        public event Action OwnershipChanged;

        public IReadOnlyList<ReinforcementZonePresenter> Zones => _zones;

        [Inject]
        private void Construct(
            IShipService shipService,
            ISquadronRegistry squadronRegistry,
            IMapModelObserver mapModel,
            IFogOfWarSystem fogOfWarSystem,
            ICameraService cameraService,
            IPointerInput pointer,
            IPlayerRoster roster,
            ILocalPlayer localPlayer,
            ReinforcementZoneData data,
            ReinforcementZoneView[] zoneViews)
        {
            _shipService = shipService;
            _squadronRegistry = squadronRegistry;
            _data = data;
            _mapModel = mapModel;
            _fogOfWarSystem = fogOfWarSystem;
            _cameraService = cameraService;
            _pointer = pointer;
            _zoneViews = zoneViews;
            _roster = roster;
            _localPlayer = localPlayer;
            _tally = new CaptureStrengthBuilder(roster);
        }

        public void Initialize()
        {
            _zones.Clear();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    startingOwner: view.StartingOwner,
                    isCapturable: view.IsCapturable,
                    captureDuration: view.CaptureDuration,
                    captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip,
                    relations: _roster);
                _zones.Add(new ReinforcementZonePresenter(model: model, view: view, localPlayer: _localPlayer));
            }
        }

        public void Tick()
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                _tally.Clear();
                Func<Vector3, bool> contains = zone.Contains;
                _shipService.AddShipStrength(contains, _tally);
                _squadronRegistry.AddSquadronStrength(contains, _data.SquadronCaptureWeight, _tally);

                if (zone.Tick(Time.deltaTime, _tally.Build()))
                {
                    OwnershipChanged?.Invoke();
                }

                // The circle stays visible; labels and minimap markers require current vision.
                bool isRevealed = !_fogOfWarSystem.IsHidden(zone.Center, MINIMUM_ZONE_VISIBILITY);
                bool isHovered = isRevealed &&
                    zone.Contains(_cameraService.GetWorldPoint(_pointer.Position, zone.Center));
                zone.SetVisibility(isRevealed, isHovered);
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

        public void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination)
        {
            destination.Clear();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.IsCapturable && zone.Owner == owner)
                {
                    destination.Add(new Bounds(zone.Center, new Vector3(zone.Radius * 2f, 0f, zone.Radius * 2f)));
                }
            }
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
    }
}
