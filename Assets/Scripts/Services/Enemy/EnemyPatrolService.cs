using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyPatrolService
    {
        private const int SECTOR_COUNT = 6;
        private const float SAMPLE_SECONDS = 3f;
        private const float COVERAGE_SECONDS = 60f;
        private const float STALL_SECONDS = 30f;
        private readonly PlayerSlot _owner;
        private readonly IMapModelObserver _map;
        private readonly IReinforcementZoneSource _zones;
        private readonly ICaptureSiteDirectory _sites;
        private readonly IVisionService _vision;
        private readonly IBattleAreaService _areas;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRoster _players;
        private readonly HostileIntelModel _intel;
        private readonly Dictionary<string, EnemyTargetCommitment> _commitments = new Dictionary<string, EnemyTargetCommitment>();
        private readonly Dictionary<string, string> _targets = new Dictionary<string, string>();
        private readonly HashSet<string> _liveIntel = new HashSet<string>();
        private readonly List<string> _expiredIntel = new List<string>();
        private readonly HashSet<string> _visitedTargets = new HashSet<string>();
        private readonly Dictionary<string, (float Distance, float At)> _progress = new Dictionary<string, (float, float)>();
        private readonly Dictionary<string, (string Id, float Until)> _stalledTargets = new Dictionary<string, (string, float)>();

        public EnemyPatrolMemory Memory { get; }

        public EnemyPatrolService(TeamIntelRegistry registry, PlayerSlot owner, IMapModelObserver map,
            IReinforcementZoneSource zones, ICaptureSiteDirectory sites, IVisionService vision,
            IBattleAreaService areas, IEntityLocator entities, IPlayerRoster players)
        {
            Memory = registry.GetPatrol(owner.Team);
            _intel = registry.Get(owner.Team);
            _owner = owner;
            _map = map;
            _zones = zones;
            _sites = sites;
            _vision = vision;
            _areas = areas;
            _entities = entities;
            _players = players;
        }

        public void Sample(float now)
        {
            if (now < Memory.NextSampleTime) { ReportVisits(now); return; }
            Memory.NextSampleTime = now + SAMPLE_SECONDS;
            Vector2 min = _map.SizeRange.Min;
            Vector2 max = _map.SizeRange.Max;
            for (int x = 0; x < SECTOR_COUNT; x++)
                for (int z = 0; z < SECTOR_COUNT; z++)
                    Memory.Register($"sector:{x},{z}", new FormationPoint(
                        Mathf.Lerp(min.x, max.x, (x + 0.5f) / SECTOR_COUNT),
                        Mathf.Lerp(min.y, max.y, (z + 0.5f) / SECTOR_COUNT)), "sector", 1f);
            for (int i = 0; i < _zones.Zones.Count; i++)
            {
                var zone = _zones.Zones[i];
                if (zone.IsCapturable) Memory.Register($"relay:{i}", ToPoint(zone.Center), "relay", 3f);
            }
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                bool alliedMine = site.Kind == CaptureSiteKind.Mining && _players.IsAllied(_owner.Id, site.Owner);
                Memory.Register($"site:{i}", ToPoint(site.Center), alliedMine ? "alliedMine" :
                    site.Kind == CaptureSiteKind.Mining ? "mine" : "satellite", alliedMine ? 5f : 3f);
            }
            _liveIntel.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.ShipClass != ShipClass.Structure || entity.HealthModel.IsDestroyed ||
                    !_players.IsHostile(_owner.Id, entity.Owner)) continue;
                string id = $"structure:{entity.Id}";
                _liveIntel.Add(id);
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (_vision.IsVisible(_owner.Id, position) || entity.IsPlayerBase())
                    Memory.Register(id, ToPoint(position), "hostileStructure", 2f);
            }
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (_intel.GetConfidence(sighting, now) <= 0f) continue;
                bool structure = _entities.TryGetEntity(sighting.EntityId, out IEntity entity) &&
                    entity.HealthModel.ShipClass == ShipClass.Structure;
                EnemyPatrolPoint point = Memory.Register($"intel:{sighting.EntityId}", sighting.Position,
                    structure ? "hostileStructure" : "intel", structure ? 2f : 1f);
                _liveIntel.Add(point.Id);
                if (sighting.SeenAt > point.LastSeenTime) Memory.Observe(point.Id, sighting.SeenAt);
            }
            _expiredIntel.Clear();
            foreach (EnemyPatrolPoint point in Memory.Entries)
            {
                if ((point.Id.StartsWith("intel:") || point.Id.StartsWith("structure:")) &&
                    !_liveIntel.Contains(point.Id)) _expiredIntel.Add(point.Id);
                if (!_vision.IsVisible(_owner.Id, ToVector(point.Position))) continue;
                Memory.Observe(point.Id, now);
            }
            foreach (string expired in _expiredIntel) Memory.Remove(expired);
            ReportVisits(now);
            if (now >= Memory.NextCoverageTime)
            {
                Memory.NextCoverageTime = now + COVERAGE_SECONDS;
                int seen = 0, total = 0;
                float maxStaleness = 0f, poiMax = 0f;
                string stalest = "none";
                foreach (EnemyPatrolPoint point in Memory.Entries)
                {
                    float age = point.Staleness(now);
                    if (point.IsSector)
                    {
                        total++;
                        if (point.LastSeenTime >= 0f) seen++;
                        maxStaleness = Mathf.Max(maxStaleness, age);
                    }
                    else if (age > poiMax) { poiMax = age; stalest = point.Id; }
                }
                Log("patrolCoverage", $"sectorsSeen={seen}/{total};maxStaleness={maxStaleness:F1};poiMaxStaleness={poiMax:F1};stalestPoi={stalest}");
            }
        }

        public bool TryGetTarget(string unit, Vector3 origin, Func<Vector3, bool> isSafe, out Vector3 target)
        {
            float now = Time.time;
            Sample(now);
            FormationPoint start = ToPoint(origin);
            EnemyPatrolPoint current = null;
            if (_targets.TryGetValue(unit, out string currentId)) Memory.TryGet(currentId, out current);
            bool stalled = false;
            if (current != null && _progress.TryGetValue(unit, out var progress))
            {
                float distance = current.Travel(start);
                if (distance < progress.Distance - 1f) _progress[unit] = (distance, now);
                else stalled = now - progress.At >= STALL_SECONDS;
            }
            if (stalled) _stalledTargets[unit] = (current.Id, now + STALL_SECONDS);
            _stalledTargets.TryGetValue(unit, out var blocked);
            bool Eligible(EnemyPatrolPoint point) => point.Staleness(now) > SAMPLE_SECONDS &&
                (point.Id != blocked.Id || now >= blocked.Until) &&
                !_areas.IsBlocked(_owner.Id, ToVector(point.Position)) && isSafe(ToVector(point.Position));
            EnemyPatrolPoint best = Memory.Select(start, now, Eligible, out float runnerUp);
            if (!_commitments.TryGetValue(unit, out EnemyTargetCommitment commitment))
            {
                commitment = new EnemyTargetCommitment();
                _commitments.Add(unit, commitment);
            }
            bool valid = current != null && Eligible(current);
            string selected = commitment.Select(best == null ? null : best.Id, best == null ? 0f : best.Score(start, now),
                current == null ? null : current.Id, current == null ? 0f : current.Score(start, now), valid, now,
                stalled ? "stalled" : current != null && current.Staleness(now) <= SAMPLE_SECONDS ? "visited" : "unsafe");
            target = default;
            if (commitment.Changed)
            {
                _visitedTargets.Remove(unit);
                Log("objectiveSwitch", $"domain=patrol;fromId={commitment.LastFromId};toId={commitment.LastToId};" +
                    $"fromScore={commitment.LastFromScore:F3};toScore={commitment.LastToScore:F3};margin={EnemyTargetCommitment.SWITCH_MARGIN:F2};" +
                    $"heldSeconds={commitment.LastHeldSeconds:F1};cause={commitment.LastCause}");
            }
            if (selected == null) { _targets.Remove(unit); return false; }
            _targets[unit] = selected;
            Memory.TryGet(selected, out EnemyPatrolPoint chosen);
            if (commitment.Changed) _progress[unit] = (chosen.Travel(start), now);
            target = ToVector(chosen.Position);
            if (commitment.Changed)
                Log("patrolTarget", $"unit={unit};target={chosen.Id};poiKind={chosen.Kind};staleness={chosen.Staleness(now):F1};" +
                    $"value={chosen.Value:F1};travel={chosen.Travel(start):F1};score={chosen.Score(start, now):F3};runnerUp={runnerUp:F3}");
            return true;
        }

        public void Release(string unit)
        {
            _targets.Remove(unit);
            _commitments.Remove(unit);
            _visitedTargets.Remove(unit);
            _progress.Remove(unit);
            _stalledTargets.Remove(unit);
        }

        private void ReportVisits(float now)
        {
            foreach (KeyValuePair<string, string> target in _targets)
                if (!_visitedTargets.Contains(target.Key) && Memory.TryGet(target.Value, out EnemyPatrolPoint point) &&
                    point.LastSeenTime >= 0f && now - point.LastSeenTime <= SAMPLE_SECONDS &&
                    _vision.IsVisible(_owner.Id, ToVector(point.Position)))
                {
                    _visitedTargets.Add(target.Key);
                    Log("patrolVisit", $"target={point.Id};stalenessBefore={point.LastVisitStaleness:F1};unit={target.Key}");
                }
        }

        private void Log(string kind, string reason) => EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, kind, reason);
    }
}
