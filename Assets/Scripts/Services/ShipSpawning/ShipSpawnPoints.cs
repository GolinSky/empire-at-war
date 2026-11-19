using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class ShipSpawnPoints : IShipSpawnPoints
    {
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;

        private readonly IReinforcementZoneSource _zoneSource;
        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IPlayerRelations _relations;
        private readonly ReinforcementZoneData _data;
        private readonly IEntityLocator _entities;
        private readonly IShipSpawnClearance _clearance;
        private readonly List<Vector3> _stationPositions = new List<Vector3>();
        private readonly List<(Vector3 Position, float Radius, bool Station)> _targetedAnchors =
            new List<(Vector3, float, bool)>();

        private readonly List<Vector3> _anchors = new List<Vector3>();

        public ShipSpawnPoints(
            IReinforcementZoneSource zoneSource,
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IPlayerRelations relations,
            ReinforcementZoneData data,
            IEntityLocator entities, IShipSpawnClearance clearance)
        {
            _entities = entities;
            _clearance = clearance;
            _zoneSource = zoneSource;
            _zones = zones;
            _spawnRule = spawnRule;
            _relations = relations;
            _data = data;
        }

        public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            // The front line first: relays held by the owner's team, where hostiles cannot arrive.
            _anchors.Clear();
            foreach (ReinforcementZonePresenter relay in _zoneSource.Zones)
            {
                if (_relations.IsAllied(relay.Owner, owner)) _anchors.Add(relay.Center);
            }

            return TryFindOpenPosition(owner, shipType, _data.RelaySpawnBlockRadius, out position) ||
                   TryGetDefaultZoneSpawnPosition(owner, shipType, out position);
        }

        public bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position)
        {
            _targetedAnchors.Clear();
            CollectStationPositions(owner);
            foreach (Vector3 station in _stationPositions)
                _targetedAnchors.Add((station, _data.HomeAreaRadius, true));
            foreach (ReinforcementZonePresenter relay in _zoneSource.Zones)
                if (_relations.IsAllied(relay.Owner, owner))
                    _targetedAnchors.Add((relay.Center, _data.RelaySpawnBlockRadius, false));
            if (_zones.TryGetDefaultZoneCenter(owner, out Vector3 home))
                _targetedAnchors.Add((home, _data.HomeAreaRadius, false));

            _targetedAnchors.Sort((a, b) =>
            {
                int byDistance = (a.Position - target).sqrMagnitude.CompareTo((b.Position - target).sqrMagnitude);
                return byDistance != 0 ? byDistance : b.Station.CompareTo(a.Station);
            });
            foreach (var anchor in _targetedAnchors)
            {
                _anchors.Clear();
                _anchors.Add(anchor.Position);
                if (TryFindOpenPosition(owner, shipType, anchor.Radius, out position)) return true;
            }
            position = default;
            return false;
        }

        public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            _anchors.Clear();
            if (_zones.TryGetDefaultZoneCenter(owner, out Vector3 home)) _anchors.Add(home);
            return TryFindOpenPosition(owner, shipType, _data.HomeAreaRadius, out position);
        }

        public bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position)
        {
            float radius = _data.HomeAreaRadius;
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, radius);
            Vector2 towardThreat = new Vector2(centroid.X - anchor.x, centroid.Z - anchor.z).normalized;
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                if (Vector2.Dot(offset, towardThreat) > 0f)
                    offset = Vector2.Reflect(offset, towardThreat);
                position = new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y);
                if (threats.IsSafe(position.x, position.z, 0f) &&
                    _spawnRule.CanSpawnShip(owner, shipType, position)) return true;
            }

            position = default;
            return false;
        }

        public bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            CollectStationPositions(owner);
            // The station nearest the home zone is the usual arrival point; later ones take over when it is lost or blocked.
            Vector3 reference = _zones.TryGetDefaultZoneCenter(owner, out Vector3 home) ? home : Vector3.zero;
            _stationPositions.Sort((a, b) => (a - reference).sqrMagnitude.CompareTo((b - reference).sqrMagnitude));
            foreach (Vector3 station in _stationPositions)
            {
                _anchors.Clear();
                _anchors.Add(station);
                if (TryFindOpenPosition(owner, shipType, _data.HomeAreaRadius, out position)) return true;
            }

            position = default;
            return false;
        }

        public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position)
        {
            float radius = _data.HomeAreaRadius;
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, radius);
            var threatCenter = new Vector2(centroid.X, centroid.Z);
            var fromStation = threatCenter - new Vector2(anchor.x, anchor.z);
            bool found = false;
            float bestDistance = float.NegativeInfinity;
            position = default;
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                if (Vector2.Dot(offset, fromStation) > 0f)
                    offset = Vector2.Reflect(offset, fromStation.normalized);
                var candidate = new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y);
                if (!_spawnRule.CanLaunchStationDefenseShip(owner, shipType, candidate)) continue;
                float distance = (new Vector2(candidate.x, candidate.z) - threatCenter).sqrMagnitude;
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                position = candidate;
                found = true;
            }

            return found;
        }

        public void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position) =>
            _clearance.ReserveLanding(token, owner, shipType, position);

        public void ReleaseDefenseLanding(object token) => _clearance.ReleaseLanding(token);

        private void CollectStationPositions(PlayerId owner)
        {
            _stationPositions.Clear();
            foreach (IEntity entity in _entities.Entities)
                if (entity.Owner == owner && entity.IsPlayerBase() && !entity.HealthModel.IsDestroyed)
                    _stationPositions.Add(entity.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        private bool TryFindOpenPosition(PlayerId owner, ShipType shipType, float searchRadius, out Vector3 position)
        {
            if (_anchors.Count > 0)
            {
                for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
                {
                    Vector2 offset = Random.insideUnitCircle * searchRadius;
                    position = _anchors[Random.Range(0, _anchors.Count)] + new Vector3(offset.x, 0f, offset.y);
                    position.y = 0f;
                    if (_spawnRule.CanSpawnShip(owner, shipType, position)) return true;
                }
            }

            position = default;
            return false;
        }
    }
}
