using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Hazards;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class EdgeSpawnPoints : IEdgeSpawnPoints
    {
        private const int ATTEMPTS = 256;
        private const float MINIMUM_SPACING = 40f;
        private readonly IMapModelObserver _map;
        private readonly IShipSpawnClearance _clearance;
        private readonly IHazardService _hazards;
        // Squadrons have no hull clearance; ships are kept apart by their landing reservations instead.
        private readonly List<(Vector3 Position, float Radius)> _reserved = new();
        private readonly System.Random _random = new System.Random();
        private int _firstEdge;

        public EdgeSpawnPoints(IMapModelObserver map, IShipSpawnClearance clearance, IHazardService hazards)
        {
            _map = map;
            _clearance = clearance;
            _hazards = hazards;
        }

        public void BeginBatch() { _reserved.Clear(); _firstEdge = _random.Next(4); }

        public bool TryGet(PlayerId owner, UnitTypeId unit, int group, out Vector3 position)
        {
            if (TryFind(owner, unit, group, out position)) return true;
            // Earlier units have left their spawn points; ship clearance alone still keeps spawns apart.
            _reserved.Clear();
            return TryFind(owner, unit, group, out position);
        }

        private bool TryFind(PlayerId owner, UnitTypeId unit, int group, out Vector3 position)
        {
            float radius = unit.IsShip ? _clearance.GetPlanarRadius(unit.ShipType) : MINIMUM_SPACING;
            Vector2 min = _map.SizeRange.Min + Vector2.one * (radius + MINIMUM_SPACING);
            Vector2 max = _map.SizeRange.Max - Vector2.one * (radius + MINIMUM_SPACING);
            for (int attempt = 0; attempt < ATTEMPTS; attempt++)
            {
                int edge = (_firstEdge + group + attempt / 64) % 4;
                float t = (float)_random.NextDouble();
                Vector3 candidate = edge switch
                {
                    0 => new Vector3(Mathf.Lerp(min.x, max.x, t), 0, min.y),
                    1 => new Vector3(max.x, 0, Mathf.Lerp(min.y, max.y, t)),
                    2 => new Vector3(Mathf.Lerp(min.x, max.x, t), 0, max.y),
                    _ => new Vector3(min.x, 0, Mathf.Lerp(min.y, max.y, t))
                };
                if (_hazards.BlocksReinforcement(candidate)) continue;
                if (unit.IsShip ? !_clearance.IsClear(owner, unit.ShipType, candidate) : OverlapsReserved(candidate, radius))
                    continue;
                _reserved.Add((candidate, radius));
                position = candidate;
                return true;
            }
            position = default;
            return false;
        }

        private bool OverlapsReserved(Vector3 candidate, float radius)
        {
            foreach (var reserved in _reserved)
                if (Vector3.Distance(candidate, reserved.Position) < radius + reserved.Radius + MINIMUM_SPACING)
                    return true;
            return false;
        }
    }
}
