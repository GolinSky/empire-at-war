using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Rejects disconnected or strongly unequal access over the generated lane network.</summary>
    public static class MapLayoutFairness
    {
        private const float ENDPOINT_TOLERANCE = 1f;

        public static bool IsFair(IReadOnlyList<MapStation> stations, IReadOnlyList<PlayerSlot> players,
            IReadOnlyList<MapNode> nodes, IReadOnlyList<MapLane> lanes, float maxDistanceRatio = 2f)
        {
            return IsFair(stations, players, nodes, lanes, out _, maxDistanceRatio);
        }

        public static bool IsFair(IReadOnlyList<MapStation> stations, IReadOnlyList<PlayerSlot> players,
            IReadOnlyList<MapNode> nodes, IReadOnlyList<MapLane> lanes, out string failure,
            float maxDistanceRatio = 2f)
        {
            failure = "missing default relay";
            float[,] distances = BuildDistances(nodes, lanes);
            int[] starts = new int[stations.Count];
            float[] approaches = new float[stations.Count];
            float[] main = new float[stations.Count];
            float[] mines = new float[stations.Count];
            float[] enemies = new float[stations.Count];
            for (int i = 0; i < stations.Count; i++)
            {
                starts[i] = -1;
                for (int node = 0; node < nodes.Count; node++)
                    if (nodes[node].Kind == MapNodeKind.DefaultZone && nodes[node].Owner == stations[i].Owner)
                        starts[i] = node;
                if (starts[i] < 0) return false;
                approaches[i] = PlanarGeometry.Distance(stations[i].Position, nodes[starts[i]].Center);
            }

            for (int i = 0; i < stations.Count; i++)
            {
                main[i] = mines[i] = enemies[i] = float.PositiveInfinity;
                for (int node = 0; node < nodes.Count; node++)
                {
                    float distance = approaches[i] + distances[starts[i], node];
                    if (nodes[node].IsMain) main[i] = Mathf.Min(main[i], distance);
                    if (nodes[node].Kind == MapNodeKind.MiningSite && !nodes[node].IsHomeSite)
                        mines[i] = Mathf.Min(mines[i], distance);
                }
                for (int other = 0; other < stations.Count; other++)
                    if (GetTeam(stations[i].Owner, players) != GetTeam(stations[other].Owner, players))
                        enemies[i] = Mathf.Min(enemies[i], approaches[i] + approaches[other] +
                            distances[starts[i], starts[other]]);
            }
            List<float> teamEnemies = GetTeamEnemyDistances(stations, players, enemies);
            failure = $"main={GetSpread(main):F2}, mines={GetSpread(mines):F2}, " +
                $"enemyTeams={GetSpread(teamEnemies):F2}";
            return WithinSpread(main, maxDistanceRatio) && WithinSpread(mines, maxDistanceRatio) &&
                WithinSpread(teamEnemies, maxDistanceRatio);
        }

        private static List<float> GetTeamEnemyDistances(IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, IReadOnlyList<float> distances)
        {
            Dictionary<TeamId, float> totals = new Dictionary<TeamId, float>();
            Dictionary<TeamId, int> counts = new Dictionary<TeamId, int>();
            for (int i = 0; i < stations.Count; i++)
            {
                TeamId team = GetTeam(stations[i].Owner, players);
                if (!totals.ContainsKey(team))
                {
                    totals.Add(team, 0f);
                    counts.Add(team, 0);
                }
                totals[team] += distances[i];
                counts[team]++;
            }
            // Contiguous allied sectors deliberately put inner allies farther from enemies.
            // Compare team access, while main-relay and neutral-mine fairness remain per player.
            List<float> means = new List<float>();
            foreach (KeyValuePair<TeamId, float> total in totals)
                means.Add(total.Value / counts[total.Key]);
            return means;
        }

        private static float GetSpread(IReadOnlyList<float> values)
        {
            float minimum = float.PositiveInfinity;
            float maximum = 0f;
            foreach (float value in values)
            {
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }
            return maximum / minimum;
        }

        private static float[,] BuildDistances(IReadOnlyList<MapNode> nodes, IReadOnlyList<MapLane> lanes)
        {
            float[,] distances = new float[nodes.Count, nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            for (int j = 0; j < nodes.Count; j++)
                distances[i, j] = PlanarGeometry.Distance(nodes[i].Center, nodes[j].Center) < ENDPOINT_TOLERANCE
                    ? 0f : float.PositiveInfinity;
            foreach (MapLane lane in lanes)
            {
                int from = FindNode(nodes, lane.Points[0]);
                int to = FindNode(nodes, lane.Points[lane.Points.Count - 1]);
                if (from < 0 || to < 0) continue;
                float length = 0f;
                for (int i = 1; i < lane.Points.Count; i++)
                    length += PlanarGeometry.Distance(lane.Points[i - 1], lane.Points[i]);
                distances[from, to] = distances[to, from] = Mathf.Min(distances[from, to], length);
            }
            for (int via = 0; via < nodes.Count; via++)
            for (int from = 0; from < nodes.Count; from++)
            for (int to = 0; to < nodes.Count; to++)
                distances[from, to] = Mathf.Min(distances[from, to], distances[from, via] + distances[via, to]);
            return distances;
        }

        private static int FindNode(IReadOnlyList<MapNode> nodes, Vector3 position)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (PlanarGeometry.Distance(nodes[i].Center, position) < ENDPOINT_TOLERANCE) return i;
            return -1;
        }

        private static TeamId GetTeam(PlayerId owner, IReadOnlyList<PlayerSlot> players)
        {
            foreach (PlayerSlot player in players)
                if (player.Id == owner) return player.Team;
            throw new InvalidOperationException($"Station owner {owner} is absent from the roster.");
        }

        private static bool WithinSpread(IReadOnlyList<float> values, float ratio)
        {
            float minimum = float.PositiveInfinity;
            float maximum = 0f;
            foreach (float value in values)
            {
                if (float.IsInfinity(value)) return false;
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }
            return maximum <= minimum * ratio;
        }
    }
}
