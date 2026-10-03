using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class ShipSpawnPoints : IShipSpawnPoints
    {
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;
        private const float SPAWN_EDGE_PADDING = 3f;

        private readonly IReinforcementZoneSource _zoneSource;
        private readonly IShipSpawnClearance _clearance;
        private readonly IPlayerRoster _roster;

        private readonly List<ReinforcementZonePresenter> _candidateZones = new List<ReinforcementZonePresenter>();
        private readonly List<ReinforcementZonePresenter> _capturedZones = new List<ReinforcementZonePresenter>();

        public ShipSpawnPoints(
            IReinforcementZoneSource zoneSource,
            IShipSpawnClearance clearance,
            IPlayerRoster roster)
        {
            _zoneSource = zoneSource;
            _clearance = clearance;
            _roster = roster;
        }

        public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            // Reinforcements may arrive in any zone held by the owner's team.
            _candidateZones.Clear();
            _capturedZones.Clear();
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
            {
                if (!_roster.IsAllied(zone.Owner, owner))
                {
                    continue;
                }

                _candidateZones.Add(zone);
                // AI players reinforce at their team's captured front-line zones first.
                if (zone.IsCapturable && _roster.Get(owner).IsAi)
                {
                    _capturedZones.Add(zone);
                }
            }

            if (_capturedZones.Count > 0 &&
                TryGetClearSpawnPosition(_capturedZones, owner, shipType, out position))
            {
                return true;
            }

            return TryGetClearSpawnPosition(_candidateZones, owner, shipType, out position);
        }

        public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            _candidateZones.Clear();
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
            {
                if (zone.Owner == owner)
                {
                    _candidateZones.Add(zone);
                    break;
                }
            }

            return TryGetClearSpawnPosition(_candidateZones, owner, shipType, out position);
        }

        private bool TryGetClearSpawnPosition(
            IReadOnlyList<ReinforcementZonePresenter> zones,
            PlayerId owner,
            ShipType shipType,
            out Vector3 position)
        {
            // Prefer the whole hull inside the zone. When the zone is crowded that leaves capital ships
            // almost no room, so fall back to the player placement rule: only the centre must be inside.
            return TryGetClearSpawnPosition(zones, owner, shipType, true, out position) ||
                   TryGetClearSpawnPosition(zones, owner, shipType, false, out position);
        }

        private bool TryGetClearSpawnPosition(
            IReadOnlyList<ReinforcementZonePresenter> zones,
            PlayerId owner,
            ShipType shipType,
            bool keepHullInside,
            out Vector3 position)
        {
            if (zones.Count > 0)
            {
                float hullRadius = _clearance.GetPlanarRadius(shipType);
                for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
                {
                    ReinforcementZonePresenter zone = zones[Random.Range(0, zones.Count)];
                    float insideRadius = zone.Radius - SPAWN_EDGE_PADDING - hullRadius;
                    if (insideRadius < 0f)
                    {
                        continue;
                    }

                    float radius = keepHullInside ? insideRadius : zone.Radius - SPAWN_EDGE_PADDING;
                    Vector2 offset = Random.insideUnitCircle * radius;
                    position = zone.Center + new Vector3(offset.x, 0f, offset.y);
                    position.y = 0f;
                    if (_clearance.IsClear(owner, shipType, position))
                    {
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }
    }
}
