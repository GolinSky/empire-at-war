using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Decides where asteroid fields grow: lanes and clearings stay open, every capturable zone and site
    /// is wrapped in a pocket that lanes enter through narrow gaps, and point-symmetric noise shapes
    /// the open space between into blobs (islands) and ridges (long walls).
    /// </summary>
    public sealed class FieldDensity
    {
        // Second noise octave share; the first keeps the large readable shapes.
        private const float DETAIL_WEIGHT = 0.35f;
        private const float NOISE_OFFSET_RANGE = 10000f;
        private const float SYMMETRY_CONTRAST = 1.41f;
        private const float RIDGE_STRETCH = 1.5f;

        private readonly MapGenerationSettings _settings;
        private readonly MapSizeSettings _size;
        private readonly IReadOnlyList<MapStation> _stations;
        private readonly IReadOnlyList<MapNode> _nodes;
        private readonly IReadOnlyList<MapLane> _lanes;
        private readonly Vector3 _center;
        private readonly Vector2 _shapeOffset;
        private readonly Vector2 _chokeOffset;
        private readonly Vector2 _ridgeOffset;

        public FieldDensity(
            MapGenerationSettings settings,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapLane> lanes,
            Random random)
        {
            _settings = settings;
            _size = size;
            _stations = stations;
            _nodes = nodes;
            _lanes = lanes;
            _center = MapGeometry.GetCenter(size.Bounds);
            _shapeOffset = RandomOffset(random);
            _chokeOffset = RandomOffset(random);
            _ridgeOffset = RandomOffset(random);
        }

        public bool IsField(Vector3 position)
        {
            if (IsInClearing(position))
            {
                return false;
            }

            bool isInPocket = IsInPocket(position);
            if (IsInLane(position, isInPocket))
            {
                return false;
            }

            return isInPocket ||
                GetShapeNoise(position) > _size.FieldThreshold ||
                GetRidgeNoise(position) > _size.RidgeThreshold;
        }

        // Only cell centers are tested, but field volumes and rocks spill up to one cell past them,
        // so the station clearing is widened by a cell to keep its footprint free of asteroids.
        private bool IsInClearing(Vector3 position)
        {
            foreach (MapStation station in _stations)
            {
                if (MapGeometry.Distance(position, station.Position) <
                    station.Radius + _settings.ZoneClearance + _size.FieldCellSize)
                {
                    return true;
                }
            }

            foreach (MapNode node in _nodes)
            {
                if (MapGeometry.Distance(position, node.Center) < node.Radius + _settings.ZoneClearance)
                {
                    return true;
                }
            }

            return false;
        }

        // Lane corridors breathe between full width and a chokepoint; the pinch follows its own
        // symmetric noise so both sides meet the same chokepoints. Through a pocket a lane is only
        // an entrance gap, so the pocket keeps its protective C shape.
        private bool IsInLane(Vector3 position, bool isInPocket)
        {
            float width = isInPocket
                ? _size.PocketEntranceWidth
                : Mathf.Lerp(_size.MinLaneWidth, _size.LaneWidth,
                    Mathf.SmoothStep(0f, 1f, GetSymmetricNoise(position, _chokeOffset, _size.FieldFeatureSize)));
            float halfWidth = width * 0.5f;
            foreach (MapLane lane in _lanes)
            {
                if (MapGeometry.DistanceToPolyline(position, lane.Points) < halfWidth)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInPocket(Vector3 position)
        {
            foreach (MapNode node in _nodes)
            {
                if (!node.HasPocket)
                {
                    continue;
                }

                float inner = node.Radius + _settings.ZoneClearance;
                float distance = MapGeometry.Distance(position, node.Center);
                if (distance >= inner && distance < inner + _size.PocketThickness)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetShapeNoise(Vector3 position)
        {
            return GetSymmetricNoise(position, _shapeOffset, _size.FieldFeatureSize) * (1f - DETAIL_WEIGHT) +
                GetSymmetricNoise(position, _shapeOffset * 0.5f, _size.FieldFeatureSize * 0.5f) * DETAIL_WEIGHT;
        }

        // Ridged noise peaks along thin winding lines, which read as long asteroid walls.
        private float GetRidgeNoise(Vector3 position)
        {
            float noise = GetSymmetricNoise(position, _ridgeOffset, _size.FieldFeatureSize * RIDGE_STRETCH);
            return 1f - Mathf.Abs(noise * 2f - 1f);
        }

        // Averaging a point with its reflection keeps the whole field layout point-symmetric;
        // the average of two independent samples is flatter, so its spread is restored.
        private float GetSymmetricNoise(Vector3 position, Vector2 offset, float featureSize)
        {
            Vector3 mirror = MapGeometry.Reflect(position, _center);
            float average = (GetNoise(position, offset, featureSize) + GetNoise(mirror, offset, featureSize)) * 0.5f;
            return Mathf.Clamp01(0.5f + (average - 0.5f) * SYMMETRY_CONTRAST);
        }

        private static float GetNoise(Vector3 position, Vector2 offset, float featureSize)
        {
            return Mathf.PerlinNoise(position.x / featureSize + offset.x, position.z / featureSize + offset.y);
        }

        private static Vector2 RandomOffset(Random random)
        {
            return new Vector2(
                (float)random.NextDouble() * NOISE_OFFSET_RANGE,
                (float)random.NextDouble() * NOISE_OFFSET_RANGE);
        }
    }
}
