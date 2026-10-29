using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    public sealed class HazardPlacer
    {
        private const int HAZARD_SALT = 0x4E454255;
        private const int MAX_PAIR_ATTEMPTS = 6000;
        private const int SINGLE_PAIR_ATTEMPTS = 600;
        private const int SINGLE_PAIR_SWEEP_ATTEMPTS = 128;
        private const int MAX_CANDIDATE_PAIRS = 64;
        private const float RELAY_SEGMENT_MIN_SHARE = 0.35f;
        private const float RELAY_SEGMENT_MAX_SHARE = 0.65f;
        // Small-map islands are shorter and fuller than streams so they fit between the team lanes.
        private const float ISLAND_LENGTH_SCALE = 0.7f;
        private const float ISLAND_WIDTH_SCALE = 1.4f;
        // Keeps the map centre open between the two islands.
        private const float ISLAND_MIN_OFFSET = 0.15f;
        private const float ISLAND_MAX_OFFSET = 0.42f;
        // Distance-grid resolution as a share of the map side, clamped in metres.
        private const float CELL_SHARE = 1f / 400f;
        private const float MIN_CELL_SIZE = 8f;
        private const float MAX_CELL_SIZE = 20f;
        private readonly MapGenerationSettings _settings;
        private readonly PointOfInterestPlacer _points;

        public HazardPlacer(MapGenerationSettings settings, PointOfInterestPlacer points)
        {
            _settings = settings;
            _points = points;
        }

        public IReadOnlyList<HazardSpot> Place(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes, int seed)
        {
            List<HazardSpot> result = new List<HazardSpot>();
            if (_settings.Hazards.Count == 0 || size.HazardCount == 0)
                return result;

            Random random = new Random(seed ^ HAZARD_SALT);
            int firstPreset = random.Next(_settings.Hazards.Count);
            Vector2 middle = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector3 center = new Vector3(middle.x, 0f, middle.y);
            float side = Mathf.Min(size.Bounds.Max.x - size.Bounds.Min.x, size.Bounds.Max.y - size.Bounds.Min.y);
            float cellSize = Mathf.Clamp(side * CELL_SHARE, MIN_CELL_SIZE, MAX_CELL_SIZE);
            Vector3 splitAxis = GetSplitAxis(stations, center);
            Vector3 teamAxis = new Vector3(splitAxis.z, 0f, -splitAxis.x);
            // Relay-facing islands fold across the relay line; fallback islands retain the fair team axis.
            bool islands = size.HazardCount == 2;
            List<MapNode> relayTargets = new List<MapNode>();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Kind == MapNodeKind.CapturableZone && !nodes[i].IsMain && nodes[i].Mirror >= i)
                    relayTargets.Add(nodes[i]);
            SpaceHazardData data = _settings.Hazards[firstPreset];
            List<HazardSpot> candidates = new List<HazardSpot>();
            List<int> selected = new List<int>();
            List<bool[]> conflicts = new List<bool[]>();
            int attempts = islands ? SINGLE_PAIR_ATTEMPTS : MAX_PAIR_ATTEMPTS;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                // Search the smallest configured island explicitly when random candidates miss a narrow gap.
                int sweepIndex = attempt - (SINGLE_PAIR_ATTEMPTS - SINGLE_PAIR_SWEEP_ATTEMPTS);
                bool sweep = islands && sweepIndex >= 0;
                bool preferRelay = relayTargets.Count > 0 && attempt < attempts / 2 &&
                    candidates.Count < MAX_CANDIDATE_PAIRS;
                float length = side * (sweep ? size.HazardLengthShare.x : Range(random, size.HazardLengthShare));
                float width = side * (sweep ? size.HazardHalfWidthShare.x : Range(random, size.HazardHalfWidthShare));
                HazardSpot first;
                if (preferRelay)
                {
                    Vector3 direction = (relayTargets[attempt % relayTargets.Count].Center - center).normalized;
                    Vector3 along = new Vector3(-direction.z, 0f, direction.x);
                    float progress = Mathf.Lerp(RELAY_SEGMENT_MIN_SHARE, RELAY_SEGMENT_MAX_SHARE,
                        (float)random.NextDouble());
                    Vector3 position = Vector3.Lerp(center, relayTargets[attempt % relayTargets.Count].Center,
                        progress);
                    first = islands
                        ? HazardCloudGenerator.CreateIsland(data, position, along,
                            length * ISLAND_LENGTH_SCALE, width * ISLAND_WIDTH_SCALE, cellSize, random)
                        : HazardCloudGenerator.CreateStream(data, position, along, length, width, cellSize, random);
                }
                else if (islands)
                {
                    float offset = side * Mathf.Lerp(ISLAND_MIN_OFFSET, ISLAND_MAX_OFFSET, sweep
                        ? sweepIndex / (float)(SINGLE_PAIR_SWEEP_ATTEMPTS - 1) : (float)random.NextDouble());
                    first = HazardCloudGenerator.CreateIsland(data, center + splitAxis * offset, teamAxis,
                        length * ISLAND_LENGTH_SCALE, width * ISLAND_WIDTH_SCALE, cellSize, random);
                }
                else
                {
                    float angle = (float)random.NextDouble() * Mathf.PI;
                    first = HazardCloudGenerator.CreateStream(data, Vector3.zero,
                        new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)), length, width, cellSize, random);
                    Bounds reach = first.GetReach();
                    Vector2 minimum = size.Bounds.Min - new Vector2(reach.min.x, reach.min.z);
                    Vector2 maximum = size.Bounds.Max - new Vector2(reach.max.x, reach.max.z);
                    if (minimum.x > maximum.x || minimum.y > maximum.y) continue;
                    first = first.Translate(new Vector3(Mathf.Lerp(minimum.x, maximum.x, (float)random.NextDouble()), 0f,
                        Mathf.Lerp(minimum.y, maximum.y, (float)random.NextDouble())));
                }
                HazardSpot second = first.Mirror(center);
                if (!Fits(first, size, stations, nodes) || !Fits(second, size, stations, nodes) ||
                    first.GetClearance(second) <= 0f)
                    continue;
                candidates.Add(first);
                candidates.Add(second);
                bool[] conflictRow = new bool[conflicts.Count];
                for (int pair = 0; pair < conflictRow.Length; pair++)
                    conflictRow[pair] = Overlaps(first, candidates[pair * 2]) || Overlaps(first, candidates[pair * 2 + 1]) ||
                        Overlaps(second, candidates[pair * 2]) || Overlaps(second, candidates[pair * 2 + 1]);
                conflicts.Add(conflictRow);
                // Backtrack over valid pairs: an early wide cloud must not permanently exclude a later pair.
                if (!SelectPairs(conflicts, selected, 0, size.HazardCount / 2))
                {
                    if (candidates.Count >= MAX_CANDIDATE_PAIRS * 2) break;
                    continue;
                }
                for (int pair = 0; pair < selected.Count; pair++)
                {
                    SpaceHazardData firstData = _settings.Hazards[(firstPreset + pair) % _settings.Hazards.Count];
                    SpaceHazardData secondData = islands
                        ? _settings.Hazards[(firstPreset + 1) % _settings.Hazards.Count] : firstData;
                    result.Add(candidates[selected[pair] * 2].WithData(firstData));
                    result.Add(candidates[selected[pair] * 2 + 1].WithData(secondData));
                }
                break;
            }
            return result;
        }

        private static bool Overlaps(HazardSpot first, HazardSpot second)
        {
            Bounds a = first.GetReach();
            Bounds b = second.GetReach();
            if (a.min.x > b.max.x || a.max.x < b.min.x || a.min.z > b.max.z || a.max.z < b.min.z)
                return false;
            return first.GetClearance(second) <= 0f;
        }

        private static bool SelectPairs(IReadOnlyList<bool[]> conflicts, List<int> selected, int start, int count)
        {
            if (selected.Count == count)
                return true;
            for (int i = start; i < conflicts.Count; i++)
            {
                bool clear = true;
                foreach (int chosen in selected)
                    if (conflicts[i][chosen])
                    {
                        clear = false;
                        break;
                    }
                if (!clear) continue;
                selected.Add(i);
                if (SelectPairs(conflicts, selected, i + 1, count)) return true;
                selected.RemoveAt(selected.Count - 1);
            }
            return false;
        }

        private static Vector3 GetSplitAxis(IReadOnlyList<MapStation> stations, Vector3 center)
        {
            Vector3 teamAxis = stations.Count == 2
                ? stations[0].Position - center
                : (stations[0].Position + stations[1].Position) * 0.5f - center;
            teamAxis.y = 0f;
            // Use corner signs so mixed-faction station radii cannot skew the fair axis.
            if (stations.Count == 2)
                teamAxis = new Vector3(Mathf.Sign(teamAxis.x), 0f, Mathf.Sign(teamAxis.z));
            else if (Mathf.Abs(teamAxis.x) > Mathf.Abs(teamAxis.z))
                teamAxis = new Vector3(Mathf.Sign(teamAxis.x), 0f, 0f);
            else
                teamAxis = new Vector3(0f, 0f, Mathf.Sign(teamAxis.z));
            return new Vector3(-teamAxis.z, 0f, teamAxis.x).normalized;
        }

        private bool Fits(HazardSpot spot, MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes)
        {
            Bounds reach = spot.GetReach();
            if (reach.min.x < size.Bounds.Min.x || reach.max.x > size.Bounds.Max.x ||
                reach.min.z < size.Bounds.Min.y || reach.max.z > size.Bounds.Max.y)
                return false;
            foreach (MapStation station in stations)
                if (spot.GetClearance(station.Position) <= station.Radius + _settings.ZoneClearance)
                    return false;
            foreach (MapNode node in nodes)
                if (spot.GetClearance(node.Center) <= _points.GetOuterRadius(node, size))
                    return false;
            return true;
        }

        private static float Range(Random random, Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
        }
    }
}
