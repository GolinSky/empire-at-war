using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Connects every node with a spanning tree, adds a few extra loops, mirrors each road for
    /// point symmetry, and bends every road so no route runs straight between two nodes.
    /// </summary>
    public sealed class RoadNetworkBuilder
    {
        private const int MAX_EXTRA_ROAD_DEGREE = 3;
        // Crossing-prone edges stay possible for connectivity but lose to Gabriel edges.
        private const float NON_GABRIEL_WEIGHT = 3f;

        private readonly MapGenerationSettings _settings;

        public RoadNetworkBuilder(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public List<MapRoad> Build(IReadOnlyList<MapNode> nodes, IReadOnlyList<MapStation> stations, Random random)
        {
            bool[,] isGabriel = BuildGabrielGraph(nodes, stations);
            bool[,] isConnected = new bool[nodes.Count, nodes.Count];
            int[] degree = new int[nodes.Count];
            List<MapRoad> roads = new List<MapRoad>();

            foreach ((int from, int to) in BuildSpanningTree(nodes, isGabriel))
            {
                AddWithMirror(from, to, nodes, isConnected, degree, random, roads);
            }

            for (int from = 0; from < nodes.Count; from++)
            {
                for (int to = from + 1; to < nodes.Count; to++)
                {
                    if (!isGabriel[from, to] || isConnected[from, to] ||
                        degree[from] >= MAX_EXTRA_ROAD_DEGREE || degree[to] >= MAX_EXTRA_ROAD_DEGREE ||
                        random.NextDouble() >= _settings.ExtraRoadChance)
                    {
                        continue;
                    }

                    AddWithMirror(from, to, nodes, isConnected, degree, random, roads);
                }
            }

            return roads;
        }

        // An edge is Gabriel when no other node or station lies inside the circle spanning it,
        // so the planar set of such edges never crosses itself.
        private static bool[,] BuildGabrielGraph(IReadOnlyList<MapNode> nodes, IReadOnlyList<MapStation> stations)
        {
            bool[,] isGabriel = new bool[nodes.Count, nodes.Count];
            for (int from = 0; from < nodes.Count; from++)
            {
                for (int to = from + 1; to < nodes.Count; to++)
                {
                    Vector3 middle = (nodes[from].Center + nodes[to].Center) * 0.5f;
                    float radius = MapGeometry.Distance(nodes[from].Center, nodes[to].Center) * 0.5f;
                    bool isClear = true;
                    for (int other = 0; other < nodes.Count && isClear; other++)
                    {
                        isClear = other == from || other == to ||
                            MapGeometry.Distance(nodes[other].Center, middle) >= radius;
                    }

                    foreach (MapStation station in stations)
                    {
                        isClear &= MapGeometry.Distance(station.Position, middle) >= radius;
                    }

                    isGabriel[from, to] = isClear;
                    isGabriel[to, from] = isClear;
                }
            }

            return isGabriel;
        }

        private static List<(int from, int to)> BuildSpanningTree(IReadOnlyList<MapNode> nodes, bool[,] isGabriel)
        {
            List<(int from, int to)> edges = new List<(int from, int to)>();
            bool[] isInTree = new bool[nodes.Count];
            isInTree[0] = true;
            for (int added = 1; added < nodes.Count; added++)
            {
                float bestWeight = float.MaxValue;
                (int from, int to) bestEdge = default;
                for (int from = 0; from < nodes.Count; from++)
                {
                    if (!isInTree[from])
                    {
                        continue;
                    }

                    for (int to = 0; to < nodes.Count; to++)
                    {
                        if (isInTree[to])
                        {
                            continue;
                        }

                        float weight = MapGeometry.Distance(nodes[from].Center, nodes[to].Center) *
                            (isGabriel[from, to] ? 1f : NON_GABRIEL_WEIGHT);
                        if (weight < bestWeight)
                        {
                            bestWeight = weight;
                            bestEdge = (from, to);
                        }
                    }
                }

                isInTree[bestEdge.to] = true;
                edges.Add(bestEdge);
            }

            return edges;
        }

        private void AddWithMirror(
            int from,
            int to,
            IReadOnlyList<MapNode> nodes,
            bool[,] isConnected,
            int[] degree,
            Random random,
            List<MapRoad> roads)
        {
            // Point reflection flips the road's normal together with its direction,
            // so the same signed bend yields the mirrored shape.
            float bend = ((float)random.NextDouble() * 2f - 1f) * _settings.RoadBend;
            AddRoad(from, to, bend, nodes, isConnected, degree, roads);
            AddRoad(nodes[from].Mirror, nodes[to].Mirror, bend, nodes, isConnected, degree, roads);
        }

        private static void AddRoad(
            int from,
            int to,
            float bend,
            IReadOnlyList<MapNode> nodes,
            bool[,] isConnected,
            int[] degree,
            List<MapRoad> roads)
        {
            if (from == to || isConnected[from, to])
            {
                return;
            }

            isConnected[from, to] = true;
            isConnected[to, from] = true;
            degree[from]++;
            degree[to]++;

            Vector3 start = nodes[from].Center;
            Vector3 end = nodes[to].Center;
            Vector3 direction = end - start;
            direction.y = 0f;
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            Vector3 bendPoint = (start + end) * 0.5f + normal * bend;
            roads.Add(new MapRoad(new[] { start, bendPoint, end }));
        }
    }
}
