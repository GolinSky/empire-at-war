using System.Collections.Generic;
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

        private readonly List<Vector3> _anchors = new List<Vector3>();

        public ShipSpawnPoints(
            IReinforcementZoneSource zoneSource,
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IPlayerRelations relations,
            ReinforcementZoneData data)
        {
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
            var candidates = new List<Vector3>();
            foreach (ReinforcementZonePresenter relay in _zoneSource.Zones)
                if (_relations.IsAllied(relay.Owner, owner)) candidates.Add(relay.Center);
            bool hasHome = _zones.TryGetDefaultZoneCenter(owner, out Vector3 home);
            if (hasHome) candidates.Add(home);
            candidates.Sort((a, b) => (a - target).sqrMagnitude.CompareTo((b - target).sqrMagnitude));
            foreach (Vector3 anchor in candidates)
            {
                _anchors.Clear();
                _anchors.Add(anchor);
                float radius = hasHome && anchor == home ? _data.HomeAreaRadius : _data.RelaySpawnBlockRadius;
                if (TryFindOpenPosition(owner, shipType, radius, out position)) return true;
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
                // Sample behind the station first, then also allow clear positions on its flanks.
                if (attempt < MAX_RANDOM_SPAWN_ATTEMPTS / 2 && Vector2.Dot(offset, towardThreat) > 0f)
                    offset = Vector2.Reflect(offset, towardThreat);
                position = new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y);
                if (threats.IsSafe(position.x, position.z, 0f) &&
                    _spawnRule.CanSpawnShip(owner, shipType, position)) return true;
            }

            position = default;
            return false;
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
