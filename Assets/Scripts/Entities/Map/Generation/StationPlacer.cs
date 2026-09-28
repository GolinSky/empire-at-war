using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
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
        // Corner signs in walking order around the map; neighbours in this list share an edge.
        private static readonly Vector2[] CORNER_SIGNS =
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f)
        };

        private const int DUEL_PLAYER_COUNT = 2;
        private const int OPPOSITE_CORNER_STEP = 2;

        private readonly MapGenerationSettings _settings;

        public StationPlacer(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public MapStation[] Place(Vector2Range bounds, IReadOnlyList<PlayerSlot> players, Random random)
        {
            Vector2 center = (bounds.Min + bounds.Max) * 0.5f;
            // Every station shares the largest footprint's inset so the corners stay point-symmetric.
            float largestRadius = 0f;
            foreach (PlayerSlot player in players)
            {
                largestRadius = Mathf.Max(largestRadius, _settings.GetStationRadius(player.Faction));
            }

            Vector2 cornerOffset = (bounds.Max - bounds.Min) * 0.5f -
                Vector2.one * (largestRadius + _settings.StationEdgeDistance);
            int startCorner = random.Next(CORNER_SIGNS.Length);
            int cornerStep = players.Count == DUEL_PLAYER_COUNT ? OPPOSITE_CORNER_STEP : 1;

            List<PlayerSlot> teamOrder = new List<PlayerSlot>(players);
            // Grouped by team; inside a team players keep their slot order.
            teamOrder.Sort((first, second) => first.Team.Value != second.Team.Value
                ? first.Team.Value.CompareTo(second.Team.Value)
                : first.Id.Index.CompareTo(second.Id.Index));

            MapStation[] stations = new MapStation[teamOrder.Count];
            for (int i = 0; i < teamOrder.Count; i++)
            {
                Vector2 sign = CORNER_SIGNS[(startCorner + i * cornerStep) % CORNER_SIGNS.Length];
                Vector2 position = center + Vector2.Scale(sign, cornerOffset);
                stations[i] = CreateStation(teamOrder[i], position);
            }

            return stations;
        }

        private MapStation CreateStation(PlayerSlot player, Vector2 position)
        {
            return new MapStation(
                player.Faction,
                player.Id,
                new Vector3(position.x, _settings.StationHeight, position.y),
                _settings.GetStationRadius(player.Faction));
        }
    }
}
