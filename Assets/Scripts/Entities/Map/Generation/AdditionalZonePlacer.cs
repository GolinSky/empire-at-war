using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Places relay groups away from the main relay, with point mirrors when the roster supports them.</summary>
    public sealed class AdditionalZonePlacer
    {
        private const int MAX_ATTEMPTS = 400;
        private const int CANDIDATES_PER_PICK = 12;
        private const float FULL_TURN = 360f;
        private const float MIRROR_TOLERANCE = 1f;
        public string LastFailure { get; private set; }
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
            if (size.IsArchetypeLayout && !UseMirroredGroups(players, stations, center))
                return TryPlaceUnpaired(size, stations, center, mainPosition, random, nodes);
            for (int pair = 0; pair < size.AdditionalZonePairCount; pair++)
            {
                List<MapNode> best = null;
                float bestScore = float.MinValue;
                int valid = 0;
                int stationRejected = 0, mainRejected = 0, teamRejected = 0, groupRejected = 0;
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
                    if (size.IsArchetypeLayout && size.Archetype != MapArchetypeKind.OpenField)
                        candidate = CreateArchetypeCandidate(size, center, pair, random);
                    Vector3 mirror = PlanarGeometry.Reflect(candidate, center);
                    bool nearStation = false;
                    foreach (MapStation station in stations)
                        if (PlanarGeometry.Distance(candidate, station.Position) < size.AdditionalZoneStationDistance ||
                            PlanarGeometry.Distance(mirror, station.Position) < size.AdditionalZoneStationDistance)
                            nearStation = true;
                    if (nearStation) { stationRejected++; continue; }
                    if (PlanarGeometry.Distance(candidate, mainPosition) < size.AdditionalZoneMainDistance ||
                        PlanarGeometry.Distance(mirror, mainPosition) < size.AdditionalZoneMainDistance)
                    { mainRejected++; continue; }
                    if (MainZonePlacer.GetNearestTeam(candidate, stations, players) ==
                        MainZonePlacer.GetNearestTeam(mirror, stations, players))
                    { teamRejected++; continue; }
                    List<MapNode> group = CreatePair(candidate, center, pointId, first,
                        (float)random.NextDouble() * FULL_TURN);
                    if (!_placement.TryAddGroup(group, size, stations, nodes))
                    { groupRejected++; continue; }
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
                {
                    LastFailure = $"pair {pair}: station={stationRejected}, main={mainRejected}, " +
                        $"team={teamRejected}, group={groupRejected}";
                    return false;
                }
                nodes.AddRange(best);
            }
            return true;
        }

        private static bool UseMirroredGroups(IReadOnlyList<PlayerSlot> players,
            IReadOnlyList<MapStation> stations, Vector3 center)
        {
            if (players.Count % 2 != 0) return false;
            Dictionary<TeamId, int> counts = new Dictionary<TeamId, int>();
            foreach (PlayerSlot player in players)
            {
                if (!counts.ContainsKey(player.Team)) counts.Add(player.Team, 0);
                counts[player.Team]++;
            }
            foreach (MapStation station in stations)
            {
                Vector3 reflected = PlanarGeometry.Reflect(station.Position, center);
                bool hasMirror = false;
                foreach (MapStation other in stations)
                    if (PlanarGeometry.Distance(reflected, other.Position) < MIRROR_TOLERANCE)
                    {
                        hasMirror = true;
                        break;
                    }
                if (!hasMirror || MainZonePlacer.GetNearestTeam(station.Position, stations, players) ==
                    MainZonePlacer.GetNearestTeam(reflected, stations, players))
                    return false;
            }
            int teamSize = -1;
            foreach (int count in counts.Values)
            {
                if (teamSize < 0) teamSize = count;
                else if (count != teamSize) return false;
            }
            return true;
        }

        private bool TryPlaceUnpaired(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            Vector3 center, Vector3 mainPosition, Random random, List<MapNode> nodes)
        {
            for (int index = 0; index < size.AdditionalZonePairCount * 2; index++)
            {
                List<MapNode> best = null;
                float bestScore = float.MinValue;
                int first = nodes.Count;
                int pointId = stations.Count + 1 + index;
                int valid = 0;
                int stationRejected = 0, mainRejected = 0, groupRejected = 0;
                for (int attempt = 0; attempt < MAX_ATTEMPTS && valid < CANDIDATES_PER_PICK; attempt++)
                {
                    Vector3 candidate = CreateArchetypeCandidate(size, center, index / 2, random);
                    if (random.Next(2) == 0) candidate = PlanarGeometry.Reflect(candidate, center);
                    if (PlanarGeometry.Distance(candidate, mainPosition) < size.AdditionalZoneMainDistance)
                    { mainRejected++; continue; }
                    bool nearStation = false;
                    foreach (MapStation station in stations)
                        if (PlanarGeometry.Distance(candidate, station.Position) < size.AdditionalZoneStationDistance)
                            nearStation = true;
                    if (nearStation) { stationRejected++; continue; }
                    List<MapNode> group = CreateGroup(candidate, pointId, (float)random.NextDouble() * FULL_TURN);
                    for (int i = 0; i < group.Count; i++) group[i].Mirror = first + i;
                    if (!_placement.TryAddGroup(group, size, stations, nodes))
                    { groupRejected++; continue; }
                    nodes.RemoveRange(first, nodes.Count - first);
                    valid++;
                    float score = float.MaxValue;
                    foreach (MapNode node in nodes)
                        score = Mathf.Min(score, PlanarGeometry.Distance(candidate, node.Center));
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = group;
                }
                if (best == null)
                {
                    LastFailure = $"group {index}: station={stationRejected}, main={mainRejected}, group={groupRejected}";
                    return false;
                }
                nodes.AddRange(best);
            }
            return true;
        }

        private static Vector3 CreateArchetypeCandidate(MapSizeSettings size, Vector3 center, int pair, Random random)
        {
            float half = Mathf.Min(size.Bounds.Max.x - size.Bounds.Min.x,
                size.Bounds.Max.y - size.Bounds.Min.y) * 0.5f;
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = half * Mathf.Lerp(0.4f, 0.88f, (float)random.NextDouble());
            Vector3 local;
            switch (size.Archetype)
            {
                case MapArchetypeKind.TwinPass:
                    local = new Vector3(half * Mathf.Lerp(0.28f, 0.88f, (float)random.NextDouble()), 0f,
                        half * Mathf.Lerp(-0.85f, 0.85f, (float)random.NextDouble()));
                    break;
                case MapArchetypeKind.AsteroidBelt:
                    local = new Vector3(half * Mathf.Lerp(0.52f, 0.88f, (float)random.NextDouble()), 0f,
                        half * Mathf.Lerp(-0.85f, 0.85f, (float)random.NextDouble()));
                    break;
                case MapArchetypeKind.Islands:
                    float side = random.Next(2) == 0 ? 1f : -1f;
                    local = new Vector3(half * Mathf.Lerp(0.18f, 0.85f, (float)random.NextDouble()), 0f,
                        side * half * Mathf.Lerp(0.18f, 0.85f, (float)random.NextDouble()));
                    break;
                default:
                    local = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    break;
            }
            return center + Quaternion.Euler(0f, size.SkeletonAngle, 0f) * local;
        }

        private List<MapNode> CreateGroup(Vector3 position, int pointId, float turn)
        {
            List<MapNode> group = new List<MapNode>
            {
                new MapNode(MapNodeKind.CapturableZone, position, PlayerId.None, _radii.Zone, pointId),
                new MapNode(MapNodeKind.MiningSite, position, PlayerId.None, _radii.MiningSite, pointId)
            };
            // Pads sit evenly around the mine, inside the relay's capture area.
            float spread = _radii.MiningSite + _settings.SatelliteFootprintRadius + _settings.ZoneClearance;
            for (int i = 0; i < _settings.AdditionalZoneSatelliteCount; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn + i * FULL_TURN /
                    _settings.AdditionalZoneSatelliteCount, 0f) * Vector3.forward * spread;
                group.Add(new MapNode(MapNodeKind.SatelliteSite, position + offset,
                    PlayerId.None, _settings.SatelliteFootprintRadius, pointId));
            }
            return group;
        }

        private List<MapNode> CreatePair(Vector3 position, Vector3 center, int pointId, int first, float turn)
        {
            List<MapNode> group = CreateGroup(position, pointId, turn);
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
