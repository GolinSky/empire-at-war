using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Keeps only main-to-additional relay shortcuts obstructed without cutting their lane routes.</summary>
    public sealed class MainLineBlocker
    {
        private const int SEGMENT_STEPS = 80;
        private const int WALL_VOLUME_COUNT = 7;
        private const float WALL_WIDTH_SHARE = 1.25f;
        private const float WALL_BOW_SHARE = 0.3f;
        private readonly MapGenerationSettings _settings;
        private readonly List<int> _reservedNodes = new List<int>();

        public MainLineBlocker(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public bool TryReserve(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes, List<MapLane> lanes, IReadOnlyList<HazardShape> hazards,
            out List<FieldVolume> reserved)
        {
            reserved = new List<FieldVolume>();
            _reservedNodes.Clear();
            Vector3 main = PlanarGeometry.GetCenter(size.Bounds);
            float mainClearance = GetMainClearance(size, nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNode node = nodes[i];
                if (node.Kind != MapNodeKind.CapturableZone || node.IsMain || node.Mirror < i)
                    continue;
                float endClearance = node.Radius + _settings.ZoneClearance + size.PocketThickness;
                if (IsBlocked(main, node.Center, mainClearance, endClearance, size.LaneWidth,
                        System.Array.Empty<AsteroidField>(), hazards) &&
                    IsBlocked(main, nodes[node.Mirror].Center, mainClearance, endClearance, size.LaneWidth,
                        System.Array.Empty<AsteroidField>(), hazards))
                    continue;

                bool placed = false;
                for (int step = 1; step < SEGMENT_STEPS; step++)
                {
                    Vector3 position = Vector3.Lerp(main, node.Center, step / (float)SEGMENT_STEPS);
                    List<FieldVolume> wall = CreateWall(position, node.Center - main, size);
                    bool clear = true;
                    foreach (FieldVolume volume in wall)
                        if (!Fits(volume.Center, volume.Radius, size, stations, nodes, hazards) ||
                            !Fits(PlanarGeometry.Reflect(volume.Center, main), volume.Radius,
                                size, stations, nodes, hazards))
                        {
                            clear = false;
                            break;
                        }
                    if (!clear) continue;
                    reserved.AddRange(wall);
                    foreach (FieldVolume volume in wall)
                        reserved.Add(new FieldVolume(PlanarGeometry.Reflect(volume.Center, main), volume.Radius));
                    _reservedNodes.Add(i);
                    placed = true;
                    break;
                }
                if (!placed) return false;
            }
            return LaneObstacleRouter.TryRoute(size, lanes, reserved);
        }

        public void Fill(MapSizeSettings size, IReadOnlyList<MapNode> nodes,
            IReadOnlyList<HazardShape> hazards, IReadOnlyList<FieldVolume> reserved,
            List<AsteroidField> fields, Random random)
        {
            Vector3 main = PlanarGeometry.GetCenter(size.Bounds);
            float mainClearance = GetMainClearance(size, nodes);
            int offset = 0;
            foreach (int i in _reservedNodes)
            {
                MapNode node = nodes[i];
                List<FieldVolume> wall = new List<FieldVolume>();
                List<FieldVolume> mirror = new List<FieldVolume>();
                for (int j = 0; j < WALL_VOLUME_COUNT; j++)
                {
                    wall.Add(reserved[offset + j]);
                    mirror.Add(reserved[offset + WALL_VOLUME_COUNT + j]);
                }
                offset += WALL_VOLUME_COUNT * 2;
                float endClearance = node.Radius + _settings.ZoneClearance + size.PocketThickness;
                if (IsBlocked(main, node.Center, mainClearance, endClearance, size.LaneWidth, fields, hazards) &&
                    IsBlocked(main, nodes[node.Mirror].Center, mainClearance, endClearance, size.LaneWidth, fields, hazards))
                    continue;
                AsteroidField field = CreateField(wall, size, random);
                fields.Add(field);
                List<AsteroidSpot> mirroredRocks = new List<AsteroidSpot>();
                foreach (AsteroidSpot rock in field.Rocks)
                    mirroredRocks.Add(new AsteroidSpot(PlanarGeometry.Reflect(rock.Position, main),
                        rock.Rotation + new Vector3(0f, 180f, 0f), rock.Size, rock.Scale));
                fields.Add(new AsteroidField(mirror, mirroredRocks, _settings.FieldFloor, _settings.FieldCeiling));
            }
        }

        private static List<FieldVolume> CreateWall(Vector3 position, Vector3 direction, MapSizeSettings size)
        {
            direction = PlanarGeometry.NormalizeDirection(direction, Vector3.forward);
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            float radius = size.MinLaneWidth / 6f;
            float span = size.LaneWidth * WALL_WIDTH_SHARE - radius * 2f;
            List<FieldVolume> volumes = new List<FieldVolume>(WALL_VOLUME_COUNT);
            for (int i = 0; i < WALL_VOLUME_COUNT; i++)
            {
                float across = Mathf.Lerp(-span * 0.5f, span * 0.5f, i / (float)(WALL_VOLUME_COUNT - 1));
                // Bow toward the outer relay, leaving room around the main relay's satellite ring.
                volumes.Add(new FieldVolume(position + normal * across +
                    direction * (Mathf.Abs(across) * WALL_BOW_SHARE), radius));
            }
            return volumes;
        }

        private float GetMainClearance(MapSizeSettings size, IReadOnlyList<MapNode> nodes)
        {
            foreach (MapNode node in nodes)
                if (node.IsMain)
                    return node.Radius + _settings.ZoneClearance + size.PocketThickness;
            throw new System.InvalidOperationException("Main relay node is required for shortcut blocking.");
        }

        public static bool IsBlocked(Vector3 start, Vector3 end, float startClearance, float endClearance,
            float minimumWidth, IReadOnlyList<AsteroidField> fields, IReadOnlyList<HazardShape> hazards)
        {
            Vector3 direction = PlanarGeometry.NormalizeDirection(end - start, Vector3.forward);
            if (PlanarGeometry.Distance(start, end) <= startClearance + endClearance) return false;
            start += direction * startClearance;
            end -= direction * endClearance;
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            // Measure a continuous cross-section at the crossing, not a distant part of the same field.
            for (int step = 1; step < SEGMENT_STEPS; step++)
            {
                Vector3 sample = Vector3.Lerp(start, end, step / (float)SEGMENT_STEPS);
                foreach (AsteroidField field in fields)
                {
                    List<Vector2> intervals = new List<Vector2>();
                    foreach (FieldVolume volume in field.Volumes)
                    {
                        Vector3 delta = volume.Center - sample;
                        float along = Vector3.Dot(delta, direction);
                        if (Mathf.Abs(along) >= volume.Radius) continue;
                        float half = Mathf.Sqrt(volume.Radius * volume.Radius - along * along);
                        float across = Vector3.Dot(delta, normal);
                        intervals.Add(new Vector2(across - half, across + half));
                    }
                    intervals.Sort((first, second) => first.x.CompareTo(second.x));
                    float covered = -minimumWidth * 0.5f;
                    foreach (Vector2 interval in intervals)
                    {
                        if (interval.y < covered) continue;
                        if (interval.x > covered) break;
                        covered = Mathf.Max(covered, interval.y);
                        if (covered >= minimumWidth * 0.5f) return true;
                    }
                }
                foreach (HazardShape hazard in hazards)
                {
                    bool covered = true;
                    int steps = Mathf.CeilToInt(minimumWidth / (hazard.CellSize * 0.5f));
                    for (int across = 0; across <= steps; across++)
                        if (hazard.SignedDistance(sample + normal *
                            Mathf.Lerp(-minimumWidth * 0.5f, minimumWidth * 0.5f, across / (float)steps)) >= 0f)
                        {
                            covered = false;
                            break;
                        }
                    if (covered) return true;
                }
            }
            return false;
        }

        private bool Fits(Vector3 position, float radius, MapSizeSettings size,
            IReadOnlyList<MapStation> stations, IReadOnlyList<MapNode> nodes, IReadOnlyList<HazardShape> hazards)
        {
            if (position.x - radius < size.Bounds.Min.x || position.x + radius > size.Bounds.Max.x ||
                position.z - radius < size.Bounds.Min.y || position.z + radius > size.Bounds.Max.y) return false;
            foreach (MapStation station in stations)
                if (PlanarGeometry.Distance(position, station.Position) <
                    station.Radius + _settings.ZoneClearance + radius) return false;
            foreach (MapNode node in nodes)
            {
                float distance = PlanarGeometry.Distance(position, node.Center);
                if (distance < radius + size.MinLaneWidth * 0.5f + 2f ||
                    distance < node.Radius + _settings.ZoneClearance +
                    (node.HasPocket ? size.PocketThickness : 0f) + radius) return false;
            }
            foreach (HazardShape hazard in hazards)
                if (hazard.SignedDistance(position) < radius + size.FieldCellSize) return false;
            return true;
        }

        private AsteroidField CreateField(IReadOnlyList<FieldVolume> volumes, MapSizeSettings size, Random random)
        {
            FieldGrid grid = new FieldGrid(size.Bounds, Mathf.Min(size.FieldCellSize, volumes[0].Radius * 0.5f));
            List<int> cells = new List<int>();
            // Scatter's square jitter stays inside the reserved volumes.
            float margin = grid.CellSize * Mathf.Sqrt(0.5f);
            for (int i = 0; i < grid.Count; i++)
                foreach (FieldVolume volume in volumes)
                    if (PlanarGeometry.Distance(grid.GetCenter(i), volume.Center) <= volume.Radius - margin)
                    {
                        cells.Add(i);
                        break;
                    }
            return new AsteroidField(volumes, new RockScatter(_settings).Scatter(grid, cells, size.RockDensity, random),
                _settings.FieldFloor, _settings.FieldCeiling);
        }
    }
}
