using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    /// <summary>A cloud baked into a signed-distance grid (metres, negative inside). Membership, placement
    /// clearings, navigation, rendering and the minimap all read this one grid.</summary>
    public sealed class HazardShape : IHazardSource
    {
        // Field clearings pad by up to two Large field cells plus the fringe (~375 m); the grid reaches past
        // that so padded queries stay exact.
        private const float GRID_MARGIN = 400f;
        // A folded cloud's warp fades to zero across this band around its mirror line, keeping it seamless.
        private const float FOLD_RAMP = 60f;
        private const float MIN_GRADIENT = 0.01f;
        private readonly float[] _distances;

        public HazardSpot Spot { get; }
        public SpaceHazardData Data => Spot.Data;
        /// <summary>Planar box of the outline.</summary>
        public Bounds Bounds { get; }
        /// <summary>World position of sample (0, 0); samples are <see cref="CellSize"/> apart.</summary>
        public Vector3 GridOrigin { get; }
        public float CellSize { get; }
        public int Columns { get; }
        public int Rows { get; }

        public HazardShape(HazardSpot spot)
        {
            Spot = spot;
            CellSize = spot.CellSize;
            Bounds reach = spot.GetReach();
            Columns = Mathf.CeilToInt((reach.size.x + GRID_MARGIN * 2f) / CellSize) + 1;
            Rows = Mathf.CeilToInt((reach.size.z + GRID_MARGIN * 2f) / CellSize) + 1;
            GridOrigin = new Vector3(reach.min.x - GRID_MARGIN, 0f, reach.min.z - GRID_MARGIN);
            int count = Columns * Rows;
            float[] field = new float[count];
            bool[] inside = new bool[count];
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                {
                    int index = row * Columns + column;
                    field[index] = Evaluate(GetSamplePosition(column, row));
                    inside[index] = field[index] > 0f;
                }
            // Samples beside the outline project onto it with field / slope, which is sub-cell accurate;
            // every other sample measures to the projection of its nearest such sample.
            bool[] edge = new bool[count];
            Vector3[] edgePoints = new Vector3[count];
            for (int row = 1; row < Rows - 1; row++)
                for (int column = 1; column < Columns - 1; column++)
                {
                    int index = row * Columns + column;
                    edge[index] = inside[index] != inside[index - 1] || inside[index] != inside[index + 1] ||
                        inside[index] != inside[index - Columns] || inside[index] != inside[index + Columns];
                    if (edge[index]) edgePoints[index] = ProjectToOutline(field, column, row);
                }
            int[] nearest = new int[count];
            SquaredDistanceTransform.FindNearest(edge, Columns, Rows, nearest);
            _distances = new float[count];
            Bounds bounds = new Bounds(spot.Center, Vector3.zero);
            bool hasInside = false;
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                {
                    int index = row * Columns + column;
                    Vector3 position = GetSamplePosition(column, row);
                    float distance = nearest[index] < 0 ? GRID_MARGIN
                        : PlanarDistance(position, edgePoints[nearest[index]]);
                    _distances[index] = inside[index] ? -distance : distance;
                    if (!inside[index]) continue;
                    if (!hasInside) bounds = new Bounds(position, Vector3.zero);
                    hasInside = true;
                    bounds.Encapsulate(position);
                }
            bounds.Expand(new Vector3(CellSize * 2f, 0f, CellSize * 2f));
            Bounds = bounds;
        }

        public Vector3 GetSamplePosition(int column, int row)
        {
            return new Vector3(GridOrigin.x + column * CellSize, 0f, GridOrigin.z + row * CellSize);
        }

        public float GetDistance(int column, int row) => _distances[row * Columns + column];

        public bool Contains(Vector3 position, float footprintRadius, bool wasInside)
        {
            float padding = footprintRadius + (wasInside ? Data.ExitHysteresis : 0f);
            if (position.x < Bounds.min.x - padding || position.x > Bounds.max.x + padding ||
                position.z < Bounds.min.z - padding || position.z > Bounds.max.z + padding)
                return false;
            return SignedDistance(position) <= padding;
        }

        public float SignedDistance(Vector3 position)
        {
            float x = (position.x - GridOrigin.x) / CellSize;
            float z = (position.z - GridOrigin.z) / CellSize;
            float maxX = Columns - 1;
            float maxZ = Rows - 1;
            if (x < 0f || z < 0f || x > maxX || z > maxZ)
            {
                // The outline sits at least GRID_MARGIN inside the grid edge.
                float dx = Mathf.Max(0f, Mathf.Max(-x, x - maxX));
                float dz = Mathf.Max(0f, Mathf.Max(-z, z - maxZ));
                return GRID_MARGIN + Mathf.Sqrt(dx * dx + dz * dz) * CellSize;
            }
            int column = Mathf.Min((int)x, Columns - 2);
            int row = Mathf.Min((int)z, Rows - 2);
            float u = x - column;
            float v = z - row;
            int index = row * Columns + column;
            float bottom = Mathf.Lerp(_distances[index], _distances[index + 1], u);
            float top = Mathf.Lerp(_distances[index + Columns], _distances[index + Columns + 1], u);
            return Mathf.Lerp(bottom, top, v);
        }

        private float Evaluate(Vector3 position)
        {
            HazardFrame frame = Spot.Frame;
            float roughness = Spot.Roughness;
            Vector2 noise = Vector2.zero;
            if (roughness > 0f)
            {
                Vector2 local = frame.ToLocal(position);
                float side = 1f;
                if (frame.IsFolded)
                {
                    side = Mathf.Clamp(local.x / FOLD_RAMP, -1f, 1f);
                    local.x = Mathf.Abs(local.x);
                }
                noise = local / (roughness * HazardSpot.NOISE_SIZE_SHARE) + frame.NoiseOffset;
                // Each component stays within ±1/√2 so the warp never moves a sample past WARP_SHARE.
                Vector2 warp = new Vector2(Signed(noise.x, noise.y), Signed(noise.x + 31.7f, noise.y + 17.3f)) *
                    (roughness * HazardSpot.WARP_SHARE * 0.7071f);
                warp.x *= side;
                position += frame.ToWorldDirection(warp);
            }
            float blend = roughness * HazardSpot.BLEND_SHARE;
            float hardMax = float.NegativeInfinity;
            float value = float.NegativeInfinity;
            foreach (HazardLobe lobe in Spot.Lobes)
            {
                float lobeValue = lobe.Evaluate(position);
                hardMax = Mathf.Max(hardMax, lobeValue);
                value = SmoothMax(value, lobeValue, blend);
            }
            // Chained smooth unions can stack; cap the lift at one union's worth so Spread stays conservative.
            value = Mathf.Min(value, hardMax + blend * 0.25f);
            if (roughness <= 0f) return value;
            float detail = Signed(noise.x * 2.7f + 5.1f, noise.y * 2.7f) * 0.65f +
                Signed(noise.x * 5.9f + 9.4f, noise.y * 5.9f) * 0.35f;
            return value + Mathf.Clamp(detail, -1f, 1f) * roughness * HazardSpot.DETAIL_SHARE;
        }

        private Vector3 ProjectToOutline(float[] field, int column, int row)
        {
            int index = row * Columns + column;
            Vector2 gradient = new Vector2(field[index + 1] - field[index - 1],
                field[index + Columns] - field[index - Columns]) / (2f * CellSize);
            Vector3 position = GetSamplePosition(column, row);
            float slope = gradient.sqrMagnitude;
            if (slope < MIN_GRADIENT * MIN_GRADIENT) return position;
            // One Newton step toward field = 0, capped at one cell so a kink cannot fling the point away.
            Vector2 step = Vector2.ClampMagnitude(-gradient * (field[index] / slope), CellSize);
            return new Vector3(position.x + step.x, 0f, position.z + step.y);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static float SmoothMax(float a, float b, float blend)
        {
            if (blend <= 0f || float.IsNegativeInfinity(a)) return Mathf.Max(a, b);
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / blend);
            return Mathf.Lerp(a, b, h) + blend * h * (1f - h);
        }

        private static float Signed(float x, float y)
        {
            return Mathf.Clamp(Mathf.PerlinNoise(x, y) * 2f - 1f, -1f, 1f);
        }
    }
}
