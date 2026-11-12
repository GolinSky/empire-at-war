using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Saves;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Entities.EnemyFaction.Models;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class AllyAiMarkerSource : IEnemyCaptureObjectiveSource, IInitializable, ILateDisposable, ITickable
    {
        public const float REQUEST_RADIUS = 1000f;
        private const float DISTANT_REQUEST_DISTANCE = 6000f;
        private const float RECONSIDER_SECONDS = 3f;
        private readonly TacticalMarkerRegistry _markers;
        private readonly IMatchSession _session;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;
        private readonly ForceCompositionBuilder _forces;
        private readonly IShipService _ships;
        private readonly HostileIntelModel _intel;
        private readonly EnemyStationPressureTracker _station;
        private readonly AllyAiMarkerEmitter _emitter;
        private readonly TacticalMarkerReplies _responses;
        private readonly BattleClock _clock;
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _threat = new ForceComposition();
        // Latest evaluation, which drives objectives and requests.
        private readonly Dictionary<long, TacticalMarkerReplyStatus> _replies = new Dictionary<long, TacticalMarkerReplyStatus>();
        // What the sender was told: the first answer, then only an upgrade to Accepted.
        private readonly Dictionary<long, TacticalMarkerReplyStatus> _posted = new Dictionary<long, TacticalMarkerReplyStatus>();
        // Accepted requests waiting for a task group to take them before "On my way" is sent.
        private readonly Dictionary<long, float> _awaitingGroup = new Dictionary<long, float>();
        private readonly IEntityLocator _entities;
        private readonly HashSet<long> _availableShips = new HashSet<long>();
        private float _nextEvaluation;

        public AllyAiMarkerSource(TacticalMarkerRegistry markers, IMatchSession session, IPlayerRoster players,
            PlayerSlot owner, ForceCompositionBuilder forces, IShipService ships, TeamIntelRegistry intel,
            EnemyStationPressureTracker station, AllyAiMarkerEmitter emitter, TacticalMarkerReplies responses, BattleClock clock,
            IEntityLocator entities)
        {
            _entities = entities;
            _markers = markers;
            _session = session;
            _players = players;
            _owner = owner;
            _forces = forces;
            _ships = ships;
            _intel = intel.Get(owner.Team);
            _station = station;
            _emitter = emitter;
            _responses = responses;
            _clock = clock;
        }

        public void Initialize()
        {
            _markers.Added += HandleAdded;
            _markers.Removed += HandleRemoved;
            _clock.Restored += Reset;
        }

        public void LateDispose()
        {
            _markers.Added -= HandleAdded;
            _markers.Removed -= HandleRemoved;
            _clock.Restored -= Reset;
            ClearReplies();
        }

        private void Reset()
        {
            _nextEvaluation = 0f;
            ClearReplies();
        }

        private void ClearReplies()
        {
            _replies.Clear();
            _posted.Clear();
            _awaitingGroup.Clear();
        }

        public void Tick()
        {
            if (!_session.IsAuthority || _markers.Time < _nextEvaluation) return;
            _nextEvaluation = _markers.Time + RECONSIDER_SECONDS;
            for (int i = 0; i < _markers.Markers.Count; i++)
            {
                TacticalMarker marker = _markers.Markers[i];
                if (_awaitingGroup.TryGetValue(marker.Id, out float since) &&
                    _markers.Time - since >= RECONSIDER_SECONDS)
                    Post(marker, TacticalMarkerReplyStatus.Delayed);
            }
            for (int i = 0; i < _markers.Markers.Count; i++)
            {
                TacticalMarker marker = _markers.Markers[i];
                if (IsRequest(marker)) Evaluate(marker);
            }
        }

        // Only human allies send requests; AI markers are reports for the humans.
        private bool IsRequest(TacticalMarker marker) => _session.IsAuthority && marker.Sender != _owner.Id &&
            _players.IsAllied(_owner.Id, marker.Sender) && !_players.Get(marker.Sender).IsAi;

        private void HandleAdded(TacticalMarker marker)
        {
            if (IsRequest(marker)) Evaluate(marker);
        }

        private void HandleRemoved(TacticalMarker marker)
        {
            _replies.Remove(marker.Id);
            _posted.Remove(marker.Id);
            _awaitingGroup.Remove(marker.Id);
        }

        private void Evaluate(TacticalMarker marker)
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            bool local = false;
            _availableShips.Clear();
            foreach (var ship in _ships.Ships)
            {
                if (ship.Owner != _owner.Id) continue;
                center += ship.WorldPosition;
                count++;
                _availableShips.Add(ship.EntityId);
                local |= Near(ship.WorldPosition, marker.Point);
            }
            if (count > 0) center /= count;
            _forces.Build(_ownForce, entity => _availableShips.Contains(entity.Id) &&
                (marker.Type != TacticalMarkerType.Retreat || Near(entity.GetFacade<IEntityTransformFacade>().Transform.position, marker.Point)));
            BuildThreat(marker.Point);
            TacticalMarkerReplyStatus result = AllyAiMarkerDecision.Evaluate(marker.Type, count > 0,
                CombatMatchup.Advantage(_ownForce, _threat), _station.HasPressure,
                (center - marker.Point).sqrMagnitude > DISTANT_REQUEST_DISTANCE * DISTANT_REQUEST_DISTANCE, local);
            _replies[marker.Id] = result;
            if (result != TacticalMarkerReplyStatus.Accepted)
            {
                _awaitingGroup.Remove(marker.Id);
                Post(marker, result);
            }
            else if (marker.Type == TacticalMarkerType.Look) Post(marker, result);
            else if (!_awaitingGroup.ContainsKey(marker.Id) && !IsPosted(marker, TacticalMarkerReplyStatus.Accepted))
                _awaitingGroup.Add(marker.Id, _markers.Time);
        }

        private bool IsPosted(TacticalMarker marker, TacticalMarkerReplyStatus status) =>
            _posted.TryGetValue(marker.Id, out var posted) && posted == status;

        private void Post(TacticalMarker marker, TacticalMarkerReplyStatus status)
        {
            if (_posted.TryGetValue(marker.Id, out var posted) &&
                (status != TacticalMarkerReplyStatus.Accepted || posted == TacticalMarkerReplyStatus.Accepted)) return;
            _posted[marker.Id] = status;
            _responses.Reply(marker, _owner.Id, status);
        }

        private TacticalMarker Take(TacticalMarker marker)
        {
            _awaitingGroup.Remove(marker.Id);
            Post(marker, TacticalMarkerReplyStatus.Accepted);
            return marker;
        }

        /// <summary>The hostile unit an accepted Attack marker points at, if it still exists.</summary>
        public bool TryGetAttackTarget(TacticalMarker marker, out IEntity target)
        {
            target = null;
            return marker.Type == TacticalMarkerType.Attack && marker.TargetId >= 0 &&
                   _entities.TryGetEntity(marker.TargetId, out target) && !target.HealthModel.IsDestroyed &&
                   _players.IsHostile(_owner.Id, target.Owner);
        }

        private bool Accepted(TacticalMarker marker) => IsRequest(marker) &&
            _replies.TryGetValue(marker.Id, out var reply) && reply == TacticalMarkerReplyStatus.Accepted;

        public void AddStrategicObjectives(List<EnemyCaptureObjective> objectives, HostileIntelModel intel)
        {
            foreach (TacticalMarker marker in _markers.Markers)
            {
                if (!Accepted(marker) || marker.Type != TacticalMarkerType.Capture && marker.Type != TacticalMarkerType.Defend) continue;
                AllyAiMarkerObjective.Apply(objectives, marker, _owner.Id);
            }
        }

        public void AddSquadronTargets(EnemySquadronCaptureAssignments assignments)
        {
            foreach (TacticalMarker marker in _markers.Markers)
                if (Accepted(marker) && (marker.Type == TacticalMarkerType.Capture || marker.Type == TacticalMarkerType.Defend))
                    assignments.AddTarget(marker.Point, marker.Type == TacticalMarkerType.Defend);
        }

        public bool TryGetScoutTarget(out Vector3 point)
        {
            for (int i = _markers.Markers.Count - 1; i >= 0; i--)
            {
                TacticalMarker marker = _markers.Markers[i];
                if (!Accepted(marker) || marker.Type != TacticalMarkerType.Look) continue;
                point = marker.Point;
                return true;
            }
            point = default;
            return false;
        }

        public TacticalMarker SelectRequest(EnemyStrategicContext context, bool recovering)
        {
            for (int i = _markers.Markers.Count - 1; i >= 0; i--)
            {
                TacticalMarker marker = _markers.Markers[i];
                if (!Accepted(marker) || marker.Type == TacticalMarkerType.Look) continue;
                _ownForce.Clear();
                bool local = false;
                foreach (var ship in context.Ships)
                {
                    bool near = Near(ship.WorldPosition, marker.Point);
                    local |= near;
                    if (marker.Type != TacticalMarkerType.Retreat || near) _ownForce.Add(context.ShipForces[ship]);
                }
                BuildThreat(marker.Point);
                float advantage = CombatMatchup.Advantage(_ownForce, _threat);
                if (marker.Type == TacticalMarkerType.Retreat)
                {
                    if (local && advantage < 1f && context.Snapshot.HasSafeRetreat) return Take(marker);
                    continue;
                }
                // Defending is still worth it while the home station is under pressure; going elsewhere is not.
                bool homeAllows = marker.Type == TacticalMarkerType.Defend || !context.Snapshot.HasStationPressure;
                if (!recovering && context.Ships.Count > 0 && homeAllows &&
                    context.Snapshot.LocalAdvantage >= 1f && advantage >= 1f) return Take(marker);
            }
            return null;
        }

        public void Observe(EnemyStrategicContext context, EnemyStrategicDecision decision)
            => _emitter.Observe(context, decision);

        private void BuildThreat(Vector3 point) => _forces.BuildKnown(_threat, _intel, Time.time,
            sighting => _players.IsHostile(_owner.Id, sighting.Owner) && Near(ToVector(sighting.Position), point));

        public static bool Near(Vector3 first, Vector3 second)
        {
            first.y = second.y;
            return (first - second).sqrMagnitude <= REQUEST_RADIUS * REQUEST_RADIUS;
        }
    }
}
