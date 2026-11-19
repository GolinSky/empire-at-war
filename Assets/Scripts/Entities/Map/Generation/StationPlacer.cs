using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Keeps legacy corners and places archetype stations in contiguous team sectors.
    /// </summary>
    public sealed class StationPlacer
    {
        private const int DUEL_PLAYER_COUNT = 2;
        private const int OPPOSITE_CORNER_STEP = 2;
        private const float HOME_MARGIN = 30f;
        private const float TEAM_SEPARATION_RATIO = 1.5f;
        private const float PULL_STEP = 0.05f;
        private const float MIN_PULL = 0.3f;

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

        /// <param name="groupTeams">Pulls allied corners together along their shared edge.</param>
        public MapStation[] Place(MapSizeSettings size, IReadOnlyList<PlayerSlot> players, Random random,
            bool groupTeams = false)
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

            Vector2[] positions = new Vector2[teamOrder.Count];
            int[] corners = new int[teamOrder.Count];
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
                corners[i] = corner;
                positions[i] = center + Vector2.Scale(CORNER_SIGNS[corner], cornerOffset);
            }

            if (groupTeams) PullAlliedCorners(center, corners, teamOrder, positions);
            MapStation[] stations = new MapStation[teamOrder.Count];
            for (int i = 0; i < stations.Length; i++)
                stations[i] = CreateStation(teamOrder[i], positions[i], _settings.GetStationRadius(teamOrder[i].Faction));
            return stations;
        }

        // Allied corners that share an edge slide toward each other until every enemy is clearly farther away.
        // A station only moves toward the centre along its shared edge, so its outward home-site room is kept.
        // A station with allies on both of its edges (3v1) keeps the whole layout: no pull keeps the mirrored relay pairs valid.
        private static void PullAlliedCorners(Vector2 center, int[] corners, IReadOnlyList<PlayerSlot> teamOrder,
            Vector2[] positions)
        {
            Vector2[] pullAxes = new Vector2[positions.Length];
            bool hasPull = false;
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = 0; j < positions.Length; j++)
                {
                    if (i == j || teamOrder[i].Team != teamOrder[j].Team) continue;
                    Vector2 signDelta = CORNER_SIGNS[corners[i]] - CORNER_SIGNS[corners[j]];
                    // Corners sharing an edge differ in exactly one sign; that axis runs along the edge.
                    if (signDelta.x != 0f && signDelta.y != 0f) continue;
                    if (signDelta.x != 0f) pullAxes[i].x = 1f;
                    else pullAxes[i].y = 1f;
                }
                if (pullAxes[i].x > 0f && pullAxes[i].y > 0f) return;
                hasPull |= pullAxes[i] != Vector2.zero;
            }
            if (!hasPull) return;

            Vector2[] original = (Vector2[])positions.Clone();
            for (float pull = 1f; pull > MIN_PULL && !IsTeamSeparated(positions, teamOrder); )
            {
                pull -= PULL_STEP;
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector2 offset = original[i] - center;
                    positions[i] = center + new Vector2(
                        pullAxes[i].x > 0f ? offset.x * pull : offset.x,
                        pullAxes[i].y > 0f ? offset.y * pull : offset.y);
                }
            }
        }

        private static bool IsTeamSeparated(Vector2[] positions, IReadOnlyList<PlayerSlot> teamOrder)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                float ally = float.MaxValue;
                float enemy = float.MaxValue;
                for (int j = 0; j < positions.Length; j++)
                {
                    if (i == j) continue;
                    float distance = Vector2.Distance(positions[i], positions[j]);
                    if (teamOrder[i].Team == teamOrder[j].Team) ally = Mathf.Min(ally, distance);
                    else enemy = Mathf.Min(enemy, distance);
                }
                if (ally < float.MaxValue && enemy < TEAM_SEPARATION_RATIO * ally) return false;
            }
            return true;
        }

        /// <param name="groupTeams">Widens ring gaps across team boundaries so allies sit closer than enemies.</param>
        /// <param name="separateTeams">Small-map fallback that swaps the slots at the first team boundary.</param>
        public MapStation[] PlaceSlots(MapSizeSettings size, IReadOnlyList<PlayerSlot> players,
            Random random, int planetSeed, bool groupTeams = false, bool separateTeams = false)
        {
            List<PlayerSlot> teamOrder = new List<PlayerSlot>(players);
            teamOrder.Sort((first, second) => first.Team.Value != second.Team.Value
                ? first.Team.Value.CompareTo(second.Team.Value)
                : first.Id.Index.CompareTo(second.Id.Index));
            if (separateTeams)
            {
                // Exchange the nearest slots at a team boundary, preserving every other allied neighbour.
                for (int i = 1; i < teamOrder.Count; i++)
                    if (teamOrder[i - 1].Team != teamOrder[i].Team)
                    {
                        (teamOrder[i - 1], teamOrder[i]) = (teamOrder[i], teamOrder[i - 1]);
                        break;
                    }
            }
            Vector2 center = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector2 halfSize = (size.Bounds.Max - size.Bounds.Min) * 0.5f;
            float largestRadius = 0f;
            foreach (PlayerSlot player in players)
                largestRadius = Mathf.Max(largestRadius, _settings.GetStationRadius(player.Faction));
            float homeRadius = Mathf.Max(_radii.MiningSite, _settings.SatelliteFootprintRadius);
            Vector2 available = halfSize - Vector2.one * (largestRadius + homeRadius + HOME_MARGIN);
            List<int> teamSizes = new List<int>();
            for (int i = 0; i < teamOrder.Count; i++)
                if (groupTeams && i > 0 && teamOrder[i - 1].Team == teamOrder[i].Team) teamSizes[teamSizes.Count - 1]++;
                else teamSizes.Add(1);
            Vector2[] offsets = StationSlotGeometry.Create(size, teamSizes, available, random, planetSeed);
            MapStation[] stations = new MapStation[players.Count];
            for (int i = 0; i < stations.Length; i++)
                stations[i] = CreateStation(teamOrder[i], center + offsets[i],
                    _settings.GetStationRadius(teamOrder[i].Faction));
            return stations;
        }

        public bool HasStationClearance(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players)
        {
            float homeRadius = Mathf.Max(_radii.MiningSite, _settings.SatelliteFootprintRadius);
            for (int i = 0; i < stations.Count; i++)
            for (int j = i + 1; j < stations.Count; j++)
            {
                bool allies = GetTeam(stations[i].Owner, players) == GetTeam(stations[j].Owner, players);
                float minimum = stations[i].Radius + stations[j].Radius +
                    4f * homeRadius + 2f * PointOfInterestPlacer.HOME_SITE_GAP;
                if (!allies) minimum = Mathf.Max(minimum, size.AdditionalZoneStationDistance);
                Vector3 delta = stations[i].Position - stations[j].Position;
                delta.y = 0f;
                if (delta.magnitude < minimum) return false;
            }
            return true;
        }

        private static TeamId GetTeam(PlayerId owner, IReadOnlyList<PlayerSlot> players)
        {
            foreach (PlayerSlot player in players)
                if (player.Id == owner) return player.Team;
            throw new InvalidOperationException($"Station owner {owner} is absent from the roster.");
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
