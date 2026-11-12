using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Saves;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class AllyAiMarkerEmitter : IInitializable, ILateDisposable, ITickable
    {
        private const float EMISSION_INTERVAL = 10f;
        private const float CAPTURE_REPORT_INTERVAL = 30f;
        private readonly TacticalMarkerRegistry _markers;
        private readonly TacticalMarkerReplies _responses;
        private readonly IMatchSession _session;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;
        private readonly HostileIntelModel _intel;
        private readonly UnitCombatProfileCatalog _profiles;
        private readonly IVisionService _vision;
        private readonly IReinforcementZonesSystem _zones;
        private readonly BattleClock _clock;
        private readonly IReadOnlyList<ICaptureSiteEvents> _siteEvents;
        private readonly IReadOnlyList<ICaptureSiteDirectory> _siteDirectories;
        private readonly HashSet<long> _reportedCapitals = new HashSet<long>();
        private readonly HashSet<ICaptureSite> _threatenedSites = new HashSet<ICaptureSite>();
        private float _nextEmission;
        private float _nextCaptureReport;
        private bool _hadPressure;
        private EnemyStrategicState _lastState;
        private PlaceMarkerCommand? _pendingAttack;
        private int _ownedZones;

        public AllyAiMarkerEmitter(TacticalMarkerRegistry markers, TacticalMarkerReplies responses,
            IMatchSession session, IPlayerRoster players, PlayerSlot owner, TeamIntelRegistry intel,
            UnitCombatProfileCatalog profiles, IReadOnlyList<ICaptureSiteEvents> siteEvents,
            IReadOnlyList<ICaptureSiteDirectory> siteDirectories, IVisionService vision, IReinforcementZonesSystem zones,
            BattleClock clock)
        {
            _markers = markers;
            _responses = responses;
            _session = session;
            _players = players;
            _owner = owner;
            _intel = intel.Get(owner.Team);
            _profiles = profiles;
            _siteEvents = siteEvents;
            _siteDirectories = siteDirectories;
            _vision = vision;
            _zones = zones;
            _clock = clock;
        }

        public void Initialize()
        {
            foreach (var sites in _siteEvents) sites.SiteCaptured += HandleCaptured;
            _ownedZones = _zones.GetOwnedCapturableZoneCount(_owner.Id);
            _zones.OwnershipChanged += HandleZonesChanged;
            _clock.Restored += Reset;
        }

        public void LateDispose()
        {
            foreach (var sites in _siteEvents) sites.SiteCaptured -= HandleCaptured;
            _zones.OwnershipChanged -= HandleZonesChanged;
            _clock.Restored -= Reset;
        }

        private void Reset()
        {
            _nextEmission = 0f;
            _nextCaptureReport = 0f;
            _pendingAttack = null;
            _hadPressure = false;
            _lastState = default;
            _reportedCapitals.Clear();
            _threatenedSites.Clear();
            _ownedZones = _zones.GetOwnedCapturableZoneCount(_owner.Id);
        }

        private bool CanCommunicate()
        {
            if (!_session.IsAuthority) return false;
            foreach (PlayerSlot player in _players.Players)
                if (!player.IsAi && player.Id != _owner.Id && _players.IsAllied(_owner.Id, player.Id)) return true;
            return false;
        }

        public void Tick()
        {
            if (!CanCommunicate() || _markers.Time < _nextEmission) return;
            if (_pendingAttack.HasValue)
            {
                PlaceMarkerCommand pending = _pendingAttack.Value;
                _pendingAttack = null;
                if (Emit(pending.Type, pending.Point, pending.TargetId)) return;
            }
            foreach (var directory in _siteDirectories)
                foreach (ICaptureSite site in directory.Sites)
                {
                    bool threatened = site.Owner == _owner.Id && HasThreatAt(ToVector(ToPoint(site.Center)));
                    if (!threatened) { _threatenedSites.Remove(site); continue; }
                    if (!_threatenedSites.Contains(site) && Emit(TacticalMarkerType.Defend, ToVector(ToPoint(site.Center))))
                    { _threatenedSites.Add(site); return; }
                }
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (_reportedCapitals.Contains(sighting.EntityId) || _intel.GetConfidence(sighting, Time.time) <= 0f) continue;
                ShipClass shipClass = _profiles.Get(sighting.UnitTypeId).ShipClass;
                if (shipClass != ShipClass.Capital && shipClass != ShipClass.HeavyCapital) continue;
                if (Emit(TacticalMarkerType.Look, ToVector(sighting.Position), sighting.EntityId))
                    _reportedCapitals.Add(sighting.EntityId);
                return;
            }
        }

        public void Observe(EnemyStrategicContext context, EnemyStrategicDecision decision)
        {
            if (!CanCommunicate()) return;
            if (context.Snapshot.HasStationPressure && !_hadPressure && context.OwnBase != null)
                _hadPressure = Emit(TacticalMarkerType.Defend,
                    context.OwnBase.GetFacade<IEntityTransformFacade>().Transform.position, context.OwnBase.Id);
            if (!context.Snapshot.HasStationPressure) _hadPressure = false;
            if (decision.State == _lastState) return;
            if (decision.State == EnemyStrategicState.RetreatValue)
            {
                _pendingAttack = null;
                if (context.Ships.Count > 0 && !Emit(TacticalMarkerType.Retreat, context.Ships[0].WorldPosition)) return;
            }
            else if (decision.State == EnemyStrategicState.AssaultBase || decision.State == EnemyStrategicState.HuntFleet ||
                     decision.State == EnemyStrategicState.RaidStructures)
            {
                var target = decision.State == EnemyStrategicState.AssaultBase ? context.EnemyBaseTarget :
                    decision.State == EnemyStrategicState.HuntFleet ? context.EnemyFleetTarget : context.RaidTarget;
                if (target != null && context.Ships.Count > 0 && TryKnownPoint(target, out Vector3 targetPoint))
                {
                    if (!Emit(TacticalMarkerType.Rally, context.Ships[0].WorldPosition)) return;
                    _pendingAttack = new PlaceMarkerCommand(_owner.Id, TacticalMarkerType.Attack,
                        targetPoint, target.Id);
                }
            }
            else _pendingAttack = null;
            _lastState = decision.State;
        }

        private bool HasThreatAt(Vector3 point)
        {
            foreach (HostileSighting sighting in _intel.Sightings)
                if (_intel.GetConfidence(sighting, Time.time) > 0f && AllyAiMarkerSource.Near(ToVector(sighting.Position), point)) return true;
            return false;
        }

        private bool TryKnownPoint(IEntity target, out Vector3 point)
        {
            point = target.GetFacade<IEntityTransformFacade>().Transform.position;
            if (_vision.IsVisible(_owner.Id, point)) return true;
            foreach (HostileSighting sighting in _intel.Sightings)
                if (sighting.EntityId == target.Id && _intel.GetConfidence(sighting, Time.time) > 0f)
                { point = ToVector(sighting.Position); return true; }
            point = default;
            return false;
        }

        private bool Emit(TacticalMarkerType type, Vector3 point, long targetId = -1)
        {
            if (_markers.Time < _nextEmission || !_markers.TryPlace(new PlaceMarkerCommand(_owner.Id, type, point, targetId))) return false;
            _nextEmission = _markers.Time + EMISSION_INTERVAL;
            return true;
        }

        private void HandleCaptured(PlayerId previous, PlayerId owner, SiteFacilityType type)
        {
            if (owner == _owner.Id) ReportCapture();
        }

        // Early expansion captures many sites at once; one card per interval is enough.
        private void ReportCapture()
        {
            if (!CanCommunicate() || _markers.Time < _nextCaptureReport) return;
            _nextCaptureReport = _markers.Time + CAPTURE_REPORT_INTERVAL;
            _responses.CaptureCompleted(_owner.Id);
        }

        private void HandleZonesChanged()
        {
            int count = _zones.GetOwnedCapturableZoneCount(_owner.Id);
            if (count > _ownedZones) ReportCapture();
            _ownedZones = count;
        }
    }
}
