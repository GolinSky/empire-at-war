using System.Collections.Generic;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Places the main relay first, then home sites and the roster-compatible additional relay groups.
    /// </summary>
    public sealed class PointOfInterestPlacer
    {
        // Survival keeps its existing three-site clusters.
        public const int SITES_PER_MINING_POINT = 3;
        public const float MINING_SITE_SPACING_SHARE = 0.25f;
        private const float MIRROR_TOLERANCE = 1f;
        internal const float HOME_SITE_GAP = 15f;

        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        public string LastFailure { get; private set; }

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
            LastFailure = "main relay";
            if (!new MainZonePlacer(_settings, _radii, this).TryPlace(size, stations, players, random, nodes))
                return false;
            Vector3 mainPosition = nodes[0].Center;
            LastFailure = "default relays";
            if (!TryAddDefaultZones(size, stations, players, center, nodes)) return false;
            LastFailure = "home sites";
            if (!TryAddHomeMining(size, stations, players, center, nodes)) return false;
            LastFailure = "additional relays";
            AdditionalZonePlacer additional = new AdditionalZonePlacer(_settings, _radii, this);
            if (!additional.TryPlace(size, stations, players, center, mainPosition, random, nodes))
            {
                LastFailure += ": " + additional.LastFailure;
                return false;
            }
            LastFailure = string.Empty;
            return true;
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
            return node.IsHomeSite ? node.Radius :
                node.Radius + _settings.ZoneClearance + (node.HasPocket ? size.PocketThickness : 0f);
        }

        internal bool IsClear(MapNode candidate, MapSizeSettings size,
            IReadOnlyList<MapStation> stations, IReadOnlyList<MapNode> nodes)
        {
            float outer = GetOuterRadius(candidate, size);
            Vector3 position = candidate.Center;
            if (position.x - outer < size.Bounds.Min.x || position.x + outer > size.Bounds.Max.x ||
                position.z - outer < size.Bounds.Min.y || position.z + outer > size.Bounds.Max.y)
                return false;
            if (size.CentralObstacleRadius > 0f &&
                PlanarGeometry.Distance(position, PlanarGeometry.GetCenter(size.Bounds)) <
                    size.CentralObstacleRadius + candidate.Radius + size.MinLaneWidth * 0.5f)
                return false;
            foreach (FieldVolume wall in size.StructuralFields)
                if (PlanarGeometry.Distance(position, wall.Center) < wall.Radius + outer + size.MinLaneWidth * 0.5f)
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
            IReadOnlyList<PlayerSlot> players, Vector3 center, List<MapNode> nodes)
        {
            int first = nodes.Count;
            foreach (MapStation station in stations)
            {
                if (size.IsArchetypeLayout)
                {
                    if (!TryAddFacingDefaultZone(size, stations, players, station, center, nodes))
                        return false;
                    continue;
                }
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

        private bool TryAddFacingDefaultZone(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, MapStation station, Vector3 center, List<MapNode> nodes)
        {
            Vector3 origin = new Vector3(station.Position.x, 0f, station.Position.z);
            Vector3 forward = (center - origin).normalized;
            float offset = station.Radius + _radii.Zone + _settings.ZoneClearance + size.DefaultZoneGap;
            for (int candidate = 0; candidate < 7; candidate++)
            {
                float turn = candidate == 0 ? 0f : ((candidate + 1) / 2) * 35f *
                    (candidate % 2 == 1 ? 1f : -1f);
                MapNode node = new MapNode(MapNodeKind.DefaultZone,
                    origin + Quaternion.Euler(0f, turn, 0f) * forward * offset,
                    station.Owner, _radii.Zone);
                if (!StationAreaClearance.HasEnemyClearance(node, size, stations, players) ||
                    !IsClear(node, size, stations, nodes)) continue;
                nodes.Add(node);
                return true;
            }
            return false;
        }

        private bool TryAddHomeMining(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, Vector3 center, List<MapNode> nodes)
        {
            int first = nodes.Count;
            for (int i = 0; i < stations.Count; i++)
            {
                MapStation station = stations[i];
                Vector3 outward = new Vector3(Mathf.Sign(station.Position.x - center.x), 0f,
                    Mathf.Sign(station.Position.z - center.z)).normalized;
                if (size.IsArchetypeLayout)
                    outward = (new Vector3(station.Position.x, 0f, station.Position.z) - center).normalized;
                bool placed = false;
                for (int side = 0; side < 2 && !placed; side++)
                {
                    Vector3 direction = side == 0 ? outward : -outward;
                    if (size.IsArchetypeLayout) direction = -direction;
                    for (int orientation = 0; orientation < 3 && !placed; orientation++)
                    {
                        float turn = (orientation - 1) * 30f;
                        float spread = size.IsArchetypeLayout ? 60f : 30f;
                        Vector3 mineDirection = Quaternion.Euler(0f, turn - spread, 0f) * direction;
                        Vector3 padDirection = Quaternion.Euler(0f, turn + spread, 0f) * direction;
                        Vector3 origin = new Vector3(station.Position.x, 0f, station.Position.z);
                        MapNode mine = new MapNode(MapNodeKind.MiningSite,
                            origin + mineDirection * (station.Radius + _radii.MiningSite + HOME_SITE_GAP),
                            PlayerId.None, _radii.MiningSite, i) { IsHomeSite = true };
                        MapNode pad = new MapNode(MapNodeKind.SatelliteSite,
                            origin + padDirection * (station.Radius + _settings.SatelliteFootprintRadius + HOME_SITE_GAP),
                            PlayerId.None, _settings.SatelliteFootprintRadius, i) { IsHomeSite = true };
                        if (size.IsArchetypeLayout &&
                            (!StationAreaClearance.HasEnemyClearance(mine, size, stations, players) ||
                             !StationAreaClearance.HasEnemyClearance(pad, size, stations, players))) continue;
                        placed = TryAddGroup(new[] { mine, pad }, size, stations, nodes);
                    }
                }
                if (!placed)
                    return false;
            }
            for (int i = first; i < nodes.Count; i++)
            {
                nodes[i].Mirror = i;
                Vector3 reflected = PlanarGeometry.Reflect(nodes[i].Center, center);
                for (int j = first; j < nodes.Count; j++)
                    if (nodes[i].Kind == nodes[j].Kind &&
                        PlanarGeometry.Distance(reflected, nodes[j].Center) < MIRROR_TOLERANCE)
                        nodes[i].Mirror = j;
            }
            return true;
        }

        private static void PairWithMirroredStations(IReadOnlyList<MapStation> stations,
            Vector3 center, int firstIndex, List<MapNode> nodes)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                int mirror = i;
                Vector3 reflected = PlanarGeometry.Reflect(nodes[firstIndex + i].Center, center);
                for (int j = 0; j < stations.Count; j++)
                    if (i != j && PlanarGeometry.Distance(nodes[firstIndex + j].Center, reflected) < MIRROR_TOLERANCE)
                        mirror = j;
                nodes[firstIndex + i].Mirror = firstIndex + mirror;
            }
        }
    }
}
