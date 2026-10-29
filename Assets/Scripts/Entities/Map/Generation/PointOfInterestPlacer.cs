using System.Collections.Generic;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Places the main relay first, then home sites and mirrored additional relay groups.
    /// </summary>
    public sealed class PointOfInterestPlacer
    {
        // Survival keeps its existing three-site clusters.
        public const int SITES_PER_MINING_POINT = 3;
        public const float MINING_SITE_SPACING_SHARE = 0.25f;
        private const float MIRROR_TOLERANCE = 1f;
        private const float HOME_SITE_GAP = 15f;

        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;

        public PointOfInterestPlacer(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
        }

        public bool TryPlace(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, Random random, List<MapNode> nodes)
        {
            nodes.Clear();
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            if (!new MainZonePlacer(_settings, _radii, this).TryPlace(size, stations, players, random, nodes))
                return false;
            Vector3 mainPosition = nodes[0].Center;
            return TryAddDefaultZones(size, stations, center, nodes) &&
                TryAddHomeMining(size, stations, center, nodes) &&
                new AdditionalZonePlacer(_settings, _radii, this).TryPlace(
                    size, stations, players, center, mainPosition, random, nodes);
        }

        public Vector3 PlacePlanet(MapSizeSettings size, Random random)
        {
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            Vector2 inset = (max - min) * _settings.PlanetBorderInset;
            return new Vector3(
                Mathf.Lerp(min.x + inset.x, max.x - inset.x, (float)random.NextDouble()), 0f,
                Mathf.Lerp((min.y + max.y) * 0.5f, max.y - inset.y, (float)random.NextDouble()));
        }

        public float GetOuterRadius(MapNode node, MapSizeSettings size)
        {
            return node.Radius + _settings.ZoneClearance + (node.HasPocket ? size.PocketThickness : 0f);
        }

        internal bool IsClear(MapNode candidate, MapSizeSettings size,
            IReadOnlyList<MapStation> stations, IReadOnlyList<MapNode> nodes)
        {
            float outer = GetOuterRadius(candidate, size);
            Vector3 position = candidate.Center;
            if (position.x - outer < size.Bounds.Min.x || position.x + outer > size.Bounds.Max.x ||
                position.z - outer < size.Bounds.Min.y || position.z + outer > size.Bounds.Max.y)
                return false;
            foreach (MapStation station in stations)
                if (PlanarGeometry.Distance(position, station.Position) < station.Radius + outer)
                    return false;
            foreach (MapNode node in nodes)
            {
                // A relay's own capture sites deliberately overlap its capture area.
                if (candidate.PointId >= 0 && candidate.PointId == node.PointId)
                {
                    if (candidate.Kind == MapNodeKind.CapturableZone || node.Kind == MapNodeKind.CapturableZone)
                        continue;
                    if (PlanarGeometry.Distance(position, node.Center) < candidate.Radius + node.Radius)
                        return false;
                    continue;
                }
                float spacing = candidate.HasPocket && node.HasPocket ? size.PointSpacing : 0f;
                if (PlanarGeometry.Distance(position, node.Center) < outer + GetOuterRadius(node, size) + spacing)
                    return false;
            }
            return true;
        }

        internal bool TryAddGroup(IReadOnlyList<MapNode> group, MapSizeSettings size,
            IReadOnlyList<MapStation> stations, List<MapNode> nodes)
        {
            int first = nodes.Count;
            foreach (MapNode node in group)
            {
                if (!IsClear(node, size, stations, nodes))
                {
                    nodes.RemoveRange(first, nodes.Count - first);
                    return false;
                }
                nodes.Add(node);
            }
            return true;
        }

        private bool TryAddDefaultZones(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            Vector3 center, List<MapNode> nodes)
        {
            int first = nodes.Count;
            foreach (MapStation station in stations)
            {
                float side = station.Position.x < center.x ? 1f : -1f;
                float offset = station.Radius + _radii.Zone + _settings.ZoneClearance + size.DefaultZoneGap;
                MapNode node = new MapNode(MapNodeKind.DefaultZone,
                    new Vector3(station.Position.x + side * offset, 0f, station.Position.z),
                    station.Owner, _radii.Zone);
                if (!IsClear(node, size, stations, nodes))
                    return false;
                nodes.Add(node);
            }
            PairWithMirroredStations(stations, center, first, nodes);
            return true;
        }

        private bool TryAddHomeMining(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            Vector3 center, List<MapNode> nodes)
        {
            float separation = _radii.MiningSite + _settings.SatelliteFootprintRadius + HOME_SITE_GAP;
            for (int type = 0; type < 2; type++)
            {
                int first = nodes.Count;
                for (int i = 0; i < stations.Count; i++)
                {
                    MapStation station = stations[i];
                    // Default relays use the inward X side; home mining uses the inward Z side.
                    Vector3 inward = new Vector3(0f, 0f, Mathf.Sign(center.z - station.Position.z));
                    Vector3 beside = new Vector3(Mathf.Sign(center.x - station.Position.x), 0f, 0f);
                    float radius = type == 0 ? _radii.MiningSite : _settings.SatelliteFootprintRadius;
                    float distance = station.Radius + Mathf.Max(_radii.MiningSite + size.PocketThickness,
                        _settings.SatelliteFootprintRadius) + _settings.ZoneClearance + HOME_SITE_GAP;
                    Vector3 position = new Vector3(station.Position.x, 0f, station.Position.z) +
                        inward * distance + beside * (type == 0 ? 0f : separation);
                    MapNode node = new MapNode(type == 0 ? MapNodeKind.MiningSite : MapNodeKind.SatelliteSite,
                        position, PlayerId.None, radius, i);
                    if (!IsClear(node, size, stations, nodes))
                        return false;
                    nodes.Add(node);
                }
                PairWithMirroredStations(stations, center, first, nodes);
            }
            return true;
        }

        private static void PairWithMirroredStations(IReadOnlyList<MapStation> stations,
            Vector3 center, int firstIndex, List<MapNode> nodes)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                int mirror = i;
                Vector3 reflected = PlanarGeometry.Reflect(stations[i].Position, center);
                for (int j = 0; j < stations.Count; j++)
                    if (i != j && PlanarGeometry.Distance(stations[j].Position, reflected) < MIRROR_TOLERANCE)
                        mirror = j;
                nodes[firstIndex + i].Mirror = firstIndex + mirror;
            }
        }
    }
}
