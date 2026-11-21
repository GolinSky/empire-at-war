using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Places neutral satellite pads inside lane corridors, the only space guaranteed to stay open once
    /// asteroid fields grow. Mirrored layouts get point-mirrored pairs.
    /// </summary>
    public sealed class FreeSatellitePlacer
    {
        private const int SALT = 0x46524545;
        private const int MAX_ATTEMPTS = 400;
        private const int CANDIDATES_PER_PICK = 16;
        private const float MIN_PROGRESS = 0.25f;
        private const float MAX_PROGRESS = 0.75f;

        private readonly MapGenerationSettings _settings;
        private readonly PointOfInterestPlacer _placement;

        public FreeSatellitePlacer(MapGenerationSettings settings, PointOfInterestPlacer placement)
        {
            _settings = settings;
            _placement = placement;
        }

        /// <summary>Adds exactly <see cref="MapSizeSettings.FreeSatelliteCount"/> pads to the nodes, or none.</summary>
        public bool TryPlace(MapSizeSettings size, IReadOnlyList<MapStation> stations, List<MapNode> nodes,
            IReadOnlyList<MapLane> lanes, IReadOnlyList<HazardShape> hazards, IReadOnlyList<FieldVolume> reserved,
            int seed)
        {
            int count = size.FreeSatelliteCount;
            if (count % 2 != 0)
                throw new System.InvalidOperationException($"Free satellite pads require an even count; got {count}.");
            if (count == 0) return true;
            Random random = new Random(seed ^ SALT);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            bool mirrored = IsMirrored(nodes);
            int pointId = 0;
            foreach (MapNode node in nodes)
                pointId = Mathf.Max(pointId, node.PointId + 1);
            int first = nodes.Count;
            while (nodes.Count - first < count)
            {
                List<MapNode> best = null;
                float bestScore = float.MinValue;
                int valid = 0;
                for (int attempt = 0; attempt < MAX_ATTEMPTS && valid < CANDIDATES_PER_PICK; attempt++)
                {
                    Vector3 candidate = CreateCandidate(size, lanes, random);
                    List<MapNode> group = CreateGroup(candidate, center, mirrored, pointId, nodes.Count);
                    if (!TryAdd(group, size, stations, nodes, lanes, hazards, reserved)) continue;
                    float score = float.MaxValue;
                    for (int i = 0; i < nodes.Count - group.Count; i++)
                        score = Mathf.Min(score, PlanarGeometry.Distance(candidate, nodes[i].Center));
                    foreach (MapStation station in stations)
                        score = Mathf.Min(score, PlanarGeometry.Distance(candidate, station.Position));
                    nodes.RemoveRange(nodes.Count - group.Count, group.Count);
                    valid++;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = group;
                }
                if (best == null)
                {
                    nodes.RemoveRange(first, nodes.Count - first);
                    return false;
                }
                nodes.AddRange(best);
                pointId += best.Count;
            }
            return true;
        }

        // Outer relays carry a real mirror partner only when the roster supports point symmetry.
        private static bool IsMirrored(IReadOnlyList<MapNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Kind == MapNodeKind.CapturableZone && !nodes[i].IsMain && nodes[i].Mirror != i)
                    return true;
            return false;
        }

        private List<MapNode> CreateGroup(Vector3 candidate, Vector3 center, bool mirrored, int pointId, int index)
        {
            float radius = _settings.SatelliteFootprintRadius;
            MapNode pad = new MapNode(MapNodeKind.SatelliteSite, candidate, PlayerId.None, radius, pointId)
                { Mirror = index };
            if (!mirrored) return new List<MapNode> { pad };
            pad.Mirror = index + 1;
            return new List<MapNode>
            {
                pad,
                new MapNode(MapNodeKind.SatelliteSite, PlanarGeometry.Reflect(candidate, center), PlayerId.None,
                    radius, pointId + 1) { Mirror = index }
            };
        }

        private Vector3 CreateCandidate(MapSizeSettings size, IReadOnlyList<MapLane> lanes, Random random)
        {
            IReadOnlyList<Vector3> points = lanes[random.Next(lanes.Count)].Points;
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
                length += PlanarGeometry.Distance(points[i - 1], points[i]);
            float target = length * Mathf.Lerp(MIN_PROGRESS, MAX_PROGRESS, (float)random.NextDouble());
            float lateral = GetMaxLateral(size) * ((float)random.NextDouble() * 2f - 1f);
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 start = PlanarGeometry.Flatten(points[i - 1]);
                Vector3 end = PlanarGeometry.Flatten(points[i]);
                float segment = Vector3.Distance(start, end);
                if (target > segment && i < points.Count - 1)
                {
                    target -= segment;
                    continue;
                }
                Vector3 direction = PlanarGeometry.NormalizeDirection(end - start, Vector3.forward);
                Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
                return start + direction * Mathf.Min(target, segment) + normal * lateral;
            }
            return PlanarGeometry.Flatten(points[0]);
        }

        // Corridors pinch to the minimum lane width, so pads stay inside that guaranteed opening.
        private float GetMaxLateral(MapSizeSettings size) =>
            Mathf.Max(0f, size.MinLaneWidth * 0.5f - _settings.SatelliteFootprintRadius - _settings.ZoneClearance);

        private bool TryAdd(IReadOnlyList<MapNode> group, MapSizeSettings size, IReadOnlyList<MapStation> stations,
            List<MapNode> nodes, IReadOnlyList<MapLane> lanes, IReadOnlyList<HazardShape> hazards,
            IReadOnlyList<FieldVolume> reserved)
        {
            int first = nodes.Count;
            foreach (MapNode node in group)
            {
                if (!Fits(node, size, stations, nodes, lanes, hazards, reserved))
                {
                    nodes.RemoveRange(first, nodes.Count - first);
                    return false;
                }
                nodes.Add(node);
            }
            return true;
        }

        private bool Fits(MapNode pad, MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes, IReadOnlyList<MapLane> lanes, IReadOnlyList<HazardShape> hazards,
            IReadOnlyList<FieldVolume> reserved)
        {
            Vector3 position = pad.Center;
            float outer = pad.Radius + _settings.ZoneClearance;
            if (!_placement.IsClear(pad, size, stations, nodes)) return false;
            // Neutral pads keep the neutral relay distance from every station.
            foreach (MapStation station in stations)
                if (PlanarGeometry.Distance(position, station.Position) < size.AdditionalZoneStationDistance)
                    return false;
            foreach (FieldVolume wall in reserved)
                if (PlanarGeometry.Distance(position, wall.Center) < wall.Radius + outer) return false;
            foreach (HazardShape hazard in hazards)
                if (hazard.SignedDistance(position) <= pad.Radius + hazard.CellSize) return false;
            float lateral = GetMaxLateral(size) + 1f;
            foreach (MapLane lane in lanes)
                if (PlanarGeometry.DistanceToPolyline(position, lane.Points) <= lateral)
                    return true;
            return false;
        }
    }
}
