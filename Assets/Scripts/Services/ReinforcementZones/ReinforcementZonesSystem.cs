using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using UnityEngine.Serialization;
using EmpireAtWar.Services.Vision;
using Zenject;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position, float clearance = 0f);

        void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination);

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
        IInitializable, ILateDisposable, ITickable, IObserver<BattleMap>
    {
        private IShipService _shipService;
        private ISquadronRegistry _squadronRegistry;
        private IVisionService _visionService;
        private ISpawnBlockerService _spawnBlockerService;
        private ICameraService _cameraService;
        private IPointerInput _pointerInput;
        private IMapModelObserver _mapModel;
        private INotifier<BattleMap> _battleMap;
        private IPlayerRoster _playerRoster;
        private ILocalPlayer _localPlayer;

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private ReinforcementZoneData _data;
        private IReadOnlyList<ReinforcementZoneView> _zoneViews;
        private readonly List<ZoneSpot> _homes = new List<ZoneSpot>();
        private CaptureStrengthBuilder _captureStrengthBuilder;

        [SerializeField, FormerlySerializedAs("_spawnEdgePadding"), Min(0f)] private float spawnEdgePadding = 3f;

        public event Action OwnershipChanged;

        public IReadOnlyList<ReinforcementZonePresenter> Zones => _zones;

        [Inject]
        private void Construct(
            IShipService shipService,
            ISquadronRegistry squadronRegistry,
            IMapModelObserver mapModel,
            IVisionService visionService,
            ISpawnBlockerService spawnBlockerService,
            ICameraService cameraService,
            IPointerInput pointerInput,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            INotifier<BattleMap> battleMap,
            ReinforcementZoneData data)
        {
            _shipService = shipService;
            _squadronRegistry = squadronRegistry;
            _data = data;
            _mapModel = mapModel;
            _visionService = visionService;
            _spawnBlockerService = spawnBlockerService;
            _cameraService = cameraService;
            _pointerInput = pointerInput;
            _battleMap = battleMap;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _captureStrengthBuilder = new CaptureStrengthBuilder(playerRoster);
        }

        public void Initialize()
        {
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            _zoneViews = battleMap.ZoneViews;
            _homes.Clear();
            foreach (ZoneSpot spot in battleMap.Layout.Zones)
            {
                if (!spot.IsCapturable) _homes.Add(spot);
            }

            _zones.Clear();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    startingOwner: view.StartingOwner,
                    isCapturable: view.IsCapturable,
                    captureDuration: view.CaptureDuration,
                    captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip,
                    relations: _playerRoster);
                _zones.Add(new ReinforcementZonePresenter(model: model, view: view, localPlayer: _localPlayer));
                // Each zone centre holds a relay: only its owner's team may spawn around it.
                _spawnBlockerService.Register(view.StartingOwner, view.transform, _data.RelaySpawnBlockRadius);
            }
        }

        public void Tick()
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                ReinforcementZonePresenter zone = _zones[i];
                _captureStrengthBuilder.Clear();
                Func<Vector3, bool> contains = zone.Contains;
                _shipService.AddShipStrength(contains, _captureStrengthBuilder);
                _squadronRegistry.AddSquadronStrength(contains, _data.SquadronCaptureWeight, _captureStrengthBuilder);

                if (zone.Tick(Time.deltaTime, _captureStrengthBuilder.Build()))
                {
                    _spawnBlockerService.Register(zone.Owner, _zoneViews[i].transform, _data.RelaySpawnBlockRadius);
                    OwnershipChanged?.Invoke();
                }

                // The circle stays visible; labels and minimap markers require current vision.
                bool isRevealed = _visionService.IsVisible(_localPlayer.Id, zone.Center);
                bool isHovered = isRevealed &&
                    zone.Contains(_cameraService.GetWorldPoint(_pointerInput.Position, zone.Center));
                zone.SetVisibility(isRevealed, isHovered);
            }
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
            foreach (ZoneSpot home in _homes)
            {
                if (home.Owner != owner) continue;
                position = home.Center;
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
            float homeRadius = _data.HomeAreaRadius;
            foreach (ZoneSpot home in _homes)
            {
                Vector3 offset = shipPosition - home.Center;
                if (home.Owner != owner || offset.x * offset.x + offset.z * offset.z > homeRadius * homeRadius)
                {
                    continue;
                }

                Vector3 direction = new Vector3(
                    (_mapModel.SizeRange.Min.x + _mapModel.SizeRange.Max.x) * 0.5f - home.Center.x,
                    0f,
                    (_mapModel.SizeRange.Min.y + _mapModel.SizeRange.Max.y) * 0.5f - home.Center.z);
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Vector3.right;
                }

                position = home.Center + direction.normalized *
                    (homeRadius + shipRadius + spawnEdgePadding);
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
                // Allies never take a zone from each other, so an allied zone is never a capture target.
                if (!zone.IsCapturable || _playerRoster.IsAllied(zone.Owner, owner))
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
