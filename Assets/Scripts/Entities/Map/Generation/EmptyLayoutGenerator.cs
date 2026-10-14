using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    public static class EmptyLayoutGenerator
    {
        private const float MAP_MARGIN = 250f;

        public static float GetSpacing(IReadOnlyList<SimulationFleetEntry> fleet,
            EmpireAtWar.Services.ShipSpawning.IShipSpawnClearance clearance)
        {
            float spacing = 100f;
            foreach (SimulationFleetEntry entry in fleet)
                if (entry.Unit.IsShip)
                    spacing = Mathf.Max(spacing, clearance.GetPlanarRadius(entry.Unit.ShipType) * 2f + 50f);
            return spacing;
        }

        public static Vector3 GetPosition(int side, float distance, int index, int columns, float spacing)
        {
            float sign = side == 0 ? -1f : 1f;
            return new Vector3(sign * (distance / 2f + index / columns * spacing),
                0f, (index % columns - (columns - 1) * 0.5f) * spacing);
        }

        public static MapLayout Generate(SimulationSetup setup, float spacingA, float spacingB)
        {
            float extent = Mathf.Max(GetExtent(setup.FleetA, setup.Distance, spacingA),
                GetExtent(setup.FleetB, setup.Distance, spacingB)) + MAP_MARGIN;
            // The serialized package range exposes neither a value constructor nor writable bounds.
            Vector2Range bounds = JsonUtility.FromJson<Vector2Range>(FormattableString.Invariant(
                $"{{\"<Min>k__BackingField\":{{\"x\":{-extent},\"y\":{-extent}}},\"<Max>k__BackingField\":{{\"x\":{extent},\"y\":{extent}}}}}"));
            return new MapLayout(new Dictionary<PlayerId, Vector3>
            {
                [new PlayerId(0)] = new Vector3(-setup.Distance / 2, 0, 0),
                [new PlayerId(1)] = new Vector3(setup.Distance / 2, 0, 0)
            }, Array.Empty<ZoneSpot>(), Array.Empty<SiteSpot>(), Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                bounds, Vector3.zero);
        }

        private static float GetExtent(IReadOnlyList<SimulationFleetEntry> fleet, float distance, float spacing)
        {
            int count = 0;
            foreach (SimulationFleetEntry entry in fleet) count += entry.Quantity;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            Vector3 last = GetPosition(1, distance, count - 1, columns, spacing);
            return Mathf.Max(last.x, (columns - 1) * 0.5f * spacing) + spacing * 0.5f;
        }
    }
}
