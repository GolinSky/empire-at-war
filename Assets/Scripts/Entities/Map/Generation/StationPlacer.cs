using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Gives every player a map corner. A duel uses opposite corners; larger matches walk the corners
    /// in order with teammates next to each other, so allies share a map edge.
    /// </summary>
    public sealed class StationPlacer
    {
        private const int DUEL_PLAYER_COUNT = 2;
        private const int OPPOSITE_CORNER_STEP = 2;

        // Corner signs in walking order around the map; neighbours in this list share an edge.
        private static readonly Vector2[] CORNER_SIGNS =
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f)
        };
        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;

        public StationPlacer(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
        }

        public MapStation[] Place(MapSizeSettings size, IReadOnlyList<PlayerSlot> players, Random random)
        {
            Vector2 center = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector2 halfSize = (size.Bounds.Max - size.Bounds.Min) * 0.5f;
            int startCorner = random.Next(CORNER_SIGNS.Length);
            int cornerStep = players.Count == DUEL_PLAYER_COUNT ? OPPOSITE_CORNER_STEP : 1;

            List<PlayerSlot> teamOrder = new List<PlayerSlot>(players);
            // Grouped by team; inside a team players keep their slot order.
            teamOrder.Sort((first, second) => first.Team.Value != second.Team.Value
                ? first.Team.Value.CompareTo(second.Team.Value)
                : first.Id.Index.CompareTo(second.Id.Index));

            MapStation[] stations = new MapStation[teamOrder.Count];
            Vector2[] offsets = new Vector2[teamOrder.Count];
            for (int i = 0; i < teamOrder.Count; i++)
            {
                int corner = (startCorner + i * cornerStep) % CORNER_SIGNS.Length;
                int opposite = -1;
                for (int j = 0; j < teamOrder.Count; j++)
                    if ((startCorner + j * cornerStep) % CORNER_SIGNS.Length ==
                        (corner + OPPOSITE_CORNER_STEP) % CORNER_SIGNS.Length)
                        opposite = j;

                float radius = _settings.GetStationRadius(teamOrder[i].Faction);
                float insetRadius = opposite < 0 ? radius :
                    Mathf.Max(radius, _settings.GetStationRadius(teamOrder[opposite].Faction));
                // The middle behind orientation puts sites 15 degrees from each border's normal.
                // Reserve its largest footprint on both axes before sampling outward jitter.
                float homeRadius = Mathf.Max(_radii.MiningSite, _settings.SatelliteFootprintRadius);
                float homeInset = (insetRadius + homeRadius + PointOfInterestPlacer.HOME_SITE_GAP) *
                    Mathf.Cos(15f * Mathf.Deg2Rad) + homeRadius;
                float outwardJitter = Mathf.Min(size.StationCornerJitter,
                    insetRadius + _settings.StationEdgeDistance - homeInset);
                float inwardJitter = Mathf.Min(-size.StationCornerJitter, outwardJitter);
                Vector2 jitter = new Vector2(
                    Mathf.Lerp(inwardJitter, outwardJitter, (float)random.NextDouble()),
                    Mathf.Lerp(inwardJitter, outwardJitter, (float)random.NextDouble()));
                Vector2 cornerOffset = opposite >= 0 && opposite < i ? offsets[opposite] :
                    halfSize - Vector2.one * (insetRadius + _settings.StationEdgeDistance) + jitter;
                offsets[i] = cornerOffset;
                Vector2 position = center + Vector2.Scale(CORNER_SIGNS[corner], cornerOffset);
                stations[i] = CreateStation(teamOrder[i], position, radius);
            }

            return stations;
        }

        private MapStation CreateStation(PlayerSlot player, Vector2 position, float radius)
        {
            return new MapStation(
                player.Faction,
                player.Id,
                new Vector3(position.x, _settings.StationHeight, position.y),
                radius);
        }
    }
}
