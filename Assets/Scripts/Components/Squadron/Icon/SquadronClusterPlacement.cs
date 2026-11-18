using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Chooses where a cluster's grid sits relative to its centroid so it avoids ship hulls, ship markers
    /// and grids placed before it. Obstacles live in a uniform screen grid, so queries stay local.
    /// </summary>
    internal sealed class SquadronClusterPlacement
    {
        public const int NO_CANDIDATE = -1;
        private const float CELL_SIZE = 128f;
        private const float OVERLAP_PENALTY = 600f;
        private const float MAX_DISPLACEMENT = 220f;
        private const float SECOND_DISTANCE = 1.8f;
        private const float SWITCH_GAIN_PITCHES = 0.5f;

        private static readonly Vector2[] DIRECTIONS =
        {
            new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(1f, 0f),
            new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f)
        };

        public static int CandidateCount => DIRECTIONS.Length * 2;

        private readonly List<Rect> _obstacles = new List<Rect>();
        private readonly List<int> _nodeObstacle = new List<int>();
        private readonly List<int> _nodeNext = new List<int>();
        private readonly List<int> _stamps = new List<int>();
        private int[] _heads = new int[0];
        private Rect _bounds;
        private int _columns;
        private int _rows;
        private int _stamp;

        public void Begin(Rect bounds)
        {
            _bounds = bounds;
            _columns = Mathf.Max(1, Mathf.CeilToInt(bounds.width / CELL_SIZE));
            _rows = Mathf.Max(1, Mathf.CeilToInt(bounds.height / CELL_SIZE));
            int cells = _columns * _rows;
            if (_heads.Length < cells) _heads = new int[cells];
            for (int i = 0; i < cells; i++) _heads[i] = -1;
            _obstacles.Clear();
            _nodeObstacle.Clear();
            _nodeNext.Clear();
        }

        public void AddObstacle(Rect rect)
        {
            int index = _obstacles.Count;
            _obstacles.Add(rect);
            if (_stamps.Count <= index) _stamps.Add(0);
            GetCellRange(rect, out int minX, out int minY, out int maxX, out int maxY);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int cell = y * _columns + x;
                _nodeObstacle.Add(index);
                _nodeNext.Add(_heads[cell]);
                _heads[cell] = _nodeObstacle.Count - 1;
            }
        }

        /// <summary>Returns the grid center and the candidate that produced it.</summary>
        public Vector2 Place(Vector2 centroid, Vector2 size, float pitch, float rise, float zoom, int previous,
            out int chosen)
        {
            Vector2 half = size * 0.5f;
            float gap = Mathf.Max(0f, rise - pitch * 0.5f);
            Vector2 defaultCenter = Candidate(0, centroid, half, gap);
            float limit = MAX_DISPLACEMENT * zoom + Mathf.Max(half.x, half.y);
            float areaUnit = pitch * pitch;
            float defaultCost = Overlap(defaultCenter, size) / areaUnit * OVERLAP_PENALTY;
            chosen = 0;
            if (defaultCost <= 0f) return defaultCenter;

            float best = defaultCost;
            Vector2 bestCenter = defaultCenter;
            float gain = pitch * SWITCH_GAIN_PITCHES;
            for (int i = 1; i < CandidateCount; i++)
            {
                Vector2 center = Candidate(i, centroid, half, gap);
                float displacement = Vector2.Distance(center, defaultCenter);
                if (displacement > limit) continue;
                float cost = displacement + Overlap(center, size) / areaUnit * OVERLAP_PENALTY;
                if (i == previous) cost -= gain;
                if (cost >= best) continue;
                best = cost;
                bestCenter = center;
                chosen = i;
            }

            return bestCenter;
        }

        private static Vector2 Candidate(int index, Vector2 centroid, Vector2 half, float gap)
        {
            float scale = (index & 1) == 0 ? 1f : SECOND_DISTANCE;
            Vector2 direction = DIRECTIONS[index >> 1];
            return centroid + new Vector2(direction.x * (half.x + gap), direction.y * (half.y + gap)) * scale;
        }

        private float Overlap(Vector2 center, Vector2 size)
        {
            var rect = new Rect(center - size * 0.5f, size);
            GetCellRange(rect, out int minX, out int minY, out int maxX, out int maxY);
            _stamp++;
            float area = 0f;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                for (int node = _heads[y * _columns + x]; node >= 0; node = _nodeNext[node])
                {
                    int obstacle = _nodeObstacle[node];
                    if (_stamps[obstacle] == _stamp) continue;
                    _stamps[obstacle] = _stamp;
                    Rect other = _obstacles[obstacle];
                    float width = Mathf.Min(rect.xMax, other.xMax) - Mathf.Max(rect.xMin, other.xMin);
                    float height = Mathf.Min(rect.yMax, other.yMax) - Mathf.Max(rect.yMin, other.yMin);
                    if (width > 0f && height > 0f) area += width * height;
                }
            }

            return area;
        }

        private void GetCellRange(Rect rect, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = Mathf.Clamp(Mathf.FloorToInt((rect.xMin - _bounds.xMin) / CELL_SIZE), 0, _columns - 1);
            maxX = Mathf.Clamp(Mathf.FloorToInt((rect.xMax - _bounds.xMin) / CELL_SIZE), 0, _columns - 1);
            minY = Mathf.Clamp(Mathf.FloorToInt((rect.yMin - _bounds.yMin) / CELL_SIZE), 0, _rows - 1);
            maxY = Mathf.Clamp(Mathf.FloorToInt((rect.yMax - _bounds.yMin) / CELL_SIZE), 0, _rows - 1);
        }
    }
}
