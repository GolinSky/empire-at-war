using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
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

        public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            _anchors.Clear();
            if (_zones.TryGetDefaultZoneCenter(owner, out Vector3 home)) _anchors.Add(home);
            return TryFindOpenPosition(owner, shipType, _data.HomeAreaRadius, out position);
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
