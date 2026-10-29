using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Places mirrored relay groups in opposing team halves, far from the main relay.</summary>
    public sealed class AdditionalZonePlacer
    {
        private const int MAX_ATTEMPTS = 400;
        private const int CANDIDATES_PER_PICK = 12;
        private const float FULL_TURN = 360f;
        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        private readonly PointOfInterestPlacer _placement;

        public AdditionalZonePlacer(MapGenerationSettings settings, MapFeatureRadii radii,
            PointOfInterestPlacer placement)
        {
            _settings = settings;
            _radii = radii;
            _placement = placement;
        }

        public bool TryPlace(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, Vector3 center, Vector3 mainPosition,
            Random random, List<MapNode> nodes)
        {
            for (int pair = 0; pair < size.AdditionalZonePairCount; pair++)
            {
                List<MapNode> best = null;
                float bestScore = float.MinValue;
                int valid = 0;
                int first = nodes.Count;
                int pointId = stations.Count + 1 + pair * 2;
                for (int attempt = 0; attempt < MAX_ATTEMPTS && valid < CANDIDATES_PER_PICK; attempt++)
                {
                    float outer = _radii.Zone + _settings.ZoneClearance + size.PocketThickness;
                    float edgeDepth = Mathf.Lerp(outer, size.AdditionalZoneEdgeBand,
                        (float)random.NextDouble());
                    bool vertical = random.Next(2) == 0;
                    bool low = random.Next(2) == 0;
                    Vector3 candidate = vertical
                        ? new Vector3(low ? size.Bounds.Min.x + edgeDepth : size.Bounds.Max.x - edgeDepth, 0f,
                            Mathf.Lerp(size.Bounds.Min.y + outer, size.Bounds.Max.y - outer,
                                (float)random.NextDouble()))
                        : new Vector3(Mathf.Lerp(size.Bounds.Min.x + outer, size.Bounds.Max.x - outer,
                            (float)random.NextDouble()), 0f,
                            low ? size.Bounds.Min.y + edgeDepth : size.Bounds.Max.y - edgeDepth);
                    Vector3 mirror = PlanarGeometry.Reflect(candidate, center);
                    bool nearStation = false;
                    foreach (MapStation station in stations)
                        if (PlanarGeometry.Distance(candidate, station.Position) < size.AdditionalZoneStationDistance ||
                            PlanarGeometry.Distance(mirror, station.Position) < size.AdditionalZoneStationDistance)
                            nearStation = true;
                    if (nearStation)
                        continue;
                    if (PlanarGeometry.Distance(candidate, mainPosition) < size.AdditionalZoneMainDistance ||
                        PlanarGeometry.Distance(mirror, mainPosition) < size.AdditionalZoneMainDistance ||
                        MainZonePlacer.GetNearestTeam(candidate, stations, players) ==
                        MainZonePlacer.GetNearestTeam(mirror, stations, players))
                        continue;
                    List<MapNode> group = CreatePair(candidate, center, pointId, first,
                        (float)random.NextDouble() * FULL_TURN);
                    if (!_placement.TryAddGroup(group, size, stations, nodes))
                        continue;
                    nodes.RemoveRange(first, nodes.Count - first);
                    valid++;
                    float score = PlanarGeometry.Distance(candidate, mirror);
                    foreach (MapNode node in nodes)
                        score = Mathf.Min(score, Mathf.Min(PlanarGeometry.Distance(candidate, node.Center),
                            PlanarGeometry.Distance(mirror, node.Center)));
                    foreach (MapStation station in stations)
                        score = Mathf.Min(score, Mathf.Min(PlanarGeometry.Distance(candidate, station.Position),
                            PlanarGeometry.Distance(mirror, station.Position)) - station.Radius);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = group;
                    }
                }
                if (best == null)
                    return false;
                nodes.AddRange(best);
            }
            return true;
        }

        private List<MapNode> CreatePair(Vector3 position, Vector3 center, int pointId, int first, float turn)
        {
            List<MapNode> group = new List<MapNode>
            {
                new MapNode(MapNodeKind.CapturableZone, position, PlayerId.None, _radii.Zone, pointId),
                new MapNode(MapNodeKind.MiningSite, position, PlayerId.None, _radii.MiningSite, pointId)
            };
            float spread = _radii.MiningSite + _settings.SatellitePadOffset;
            for (int i = 0; i < _settings.AdditionalZoneSatelliteCount; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn + i * FULL_TURN /
                    _settings.AdditionalZoneSatelliteCount, 0f) * Vector3.forward * spread;
                group.Add(new MapNode(MapNodeKind.SatelliteSite, position + offset,
                    PlayerId.None, _settings.SatelliteFootprintRadius, pointId));
            }
            int count = group.Count;
            for (int i = 0; i < count; i++)
            {
                MapNode original = group[i];
                original.Mirror = first + count + i;
                group.Add(new MapNode(original.Kind, PlanarGeometry.Reflect(original.Center, center),
                    original.Owner, original.Radius, pointId + 1) { Mirror = first + i });
            }
            return group;
        }
    }
}
