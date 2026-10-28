using System;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Services.Saves;
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
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Ships;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public sealed class ReinforcementZonesSystem : MonoBehaviour, IReinforcementZonesSystem, IReinforcementZoneSource,
        IInitializable, ILateDisposable, ITickable, IObserver<BattleMap>
    {
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
        private IReadOnlyList<ReinforcementZoneView> _zoneViews = Array.Empty<ReinforcementZoneView>();
        private readonly List<ZoneSpot> _homes = new List<ZoneSpot>();
        private readonly List<Transform> _homeVisionAnchors = new List<Transform>();
        private CaptureStrengthSampler _strengthSampler;

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
            _data = data;
            _mapModel = mapModel;
            _visionService = visionService;
            _spawnBlockerService = spawnBlockerService;
            _cameraService = cameraService;
            _pointerInput = pointerInput;
            _battleMap = battleMap;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _strengthSampler = new CaptureStrengthSampler(shipService, squadronRegistry, playerRoster, data.SquadronCaptureWeight);
        }

        public void Initialize()
        {
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            UnregisterMap();
        }

        public void UpdateState(BattleMap battleMap)
        {
            UnregisterMap();
            _zoneViews = battleMap.ZoneViews;
            _homes.Clear();
            foreach (ZoneSpot spot in battleMap.Layout.Zones)
            {
                if (spot.IsCapturable) continue;
                // A home lights its whole spawn area so reinforcements can always land there.
                // Anchors are reused across maps so reloading never leaks GameObjects.
                if (_homeVisionAnchors.Count == _homes.Count)
                {
                    Transform created = new GameObject().transform;
                    created.SetParent(transform, false);
                    _homeVisionAnchors.Add(created);
                }

                Transform anchor = _homeVisionAnchors[_homes.Count];
                anchor.name = $"{spot.Owner}_HomeVision";
                anchor.position = PlanarGeometry.Flatten(spot.Center);
                _visionService.Register(spot.Owner, anchor, _data.HomeAreaRadius);
                _homes.Add(spot);
            }

            _zones.Clear();
            _strengthSampler.Clear();
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    startingOwner: view.StartingOwner,
                    isCapturable: view.IsCapturable,
                    captureDuration: view.CaptureDuration,
                    captureSpeedPerNetShip: _data.CaptureSpeedPerNetShip,
                    relations: _playerRoster);
                ReinforcementZonePresenter zone = new ReinforcementZonePresenter(model: model, view: view, localPlayer: _localPlayer);
                _zones.Add(zone);
                _strengthSampler.Add(zone.Contains);
                RegisterRelay(view.transform, view.StartingOwner);
            }
        }

        public void Tick() => TickZones(Time.deltaTime);

        public void RestoreZones(IReadOnlyList<ReinforcementZoneSave> saves)
        {
            if (saves.Count != _zones.Count)
                throw new InvalidOperationException($"Saved {saves.Count} reinforcement zones; the map has {_zones.Count}.");

            _strengthSampler.Invalidate();

            for (int i = 0; i < _zones.Count; i++)
            {
                ReinforcementZoneSave save = saves[i];
                _zones[i].Restore(save.Owner, save.CapturingPlayer, save.CaptureProgress, save.IsContested);
                RegisterRelay(_zoneViews[i].transform, save.Owner);
            }

            // Visibility waits until every relay is registered: one zone's relay can reveal another.
            foreach (ReinforcementZonePresenter zone in _zones)
                zone.SetVisibility(_visionService.IsVisible(_localPlayer.Id, zone.Center), false);

            OwnershipChanged?.Invoke();
        }

        private void TickZones(float deltaTime)
        {
            _strengthSampler.Tick(deltaTime);

            for (int i = 0; i < _zones.Count; i++)
            {
                ReinforcementZonePresenter zone = _zones[i];
                if (zone.Tick(deltaTime, _strengthSampler[i]))
                {
                    RegisterRelay(_zoneViews[i].transform, zone.Owner);
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
                if (PlanarGeometry.ContainsCircle(position, zone.Center, zone.Radius + clearance))
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
                    destination.Add(PlanarGeometry.GetCircleBounds(zone.Center, zone.Radius));
                }
            }
        }

        public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position)
        {
            foreach (ZoneSpot home in _homes)
            {
                if (home.Owner != owner) continue;
                position = PlanarGeometry.Flatten(home.Center);
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
                if (home.Owner != owner || !PlanarGeometry.ContainsCircle(shipPosition, home.Center, homeRadius))
                {
                    continue;
                }

                // Leave towards the map centre.
                position = PlanarGeometry.GetPointTowards(home.Center, PlanarGeometry.GetCenter(_mapModel.SizeRange),
                    homeRadius + shipRadius + spawnEdgePadding, Vector3.right);
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

            position = PlanarGeometry.Flatten(closestZone.Center);
            return true;
        }

        // Each zone centre holds a relay: only its owner's team may spawn around it, and an owned relay gives vision.
        private void RegisterRelay(Transform relay, PlayerId owner)
        {
            _spawnBlockerService.Register(owner, relay, _data.RelaySpawnBlockRadius);
            if (owner == PlayerId.None)
                _visionService.Unregister(relay);
            else
                _visionService.Register(owner, relay, _data.RelaySpawnBlockRadius);
        }

        private void UnregisterMap()
        {
            foreach (ReinforcementZoneView view in _zoneViews)
            {
                _spawnBlockerService.Unregister(view.transform);
                _visionService.Unregister(view.transform);
            }

            foreach (Transform anchor in _homeVisionAnchors)
            {
                _visionService.Unregister(anchor);
            }
        }
    }
}
