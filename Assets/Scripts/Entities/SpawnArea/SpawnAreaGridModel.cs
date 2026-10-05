using System;

namespace EmpireAtWar.Models.SpawnArea
{
    /// <summary>
    /// Flat (XZ) raster of the reinforcement spawn rule for one team: hidden, visible-but-blocked or open.
    /// Column 0 / row 0 is the cell at the map's minimum X / Z corner.
    /// </summary>
    public sealed class SpawnAreaGridModel
    {
        public const float HIDDEN = 0f;
        public const float BLOCKED = 0.5f;
        public const float OPEN = 1f;

        private readonly float[] _cells;
        private readonly float _minX;
        private readonly float _minZ;
        private readonly float _cellSizeX;
        private readonly float _cellSizeZ;

        public int Resolution { get; }

        public SpawnAreaGridModel(int resolution, float minX, float minZ, float sizeX, float sizeZ)
        {
            Resolution = resolution;
            _cells = new float[resolution * resolution];
            _minX = minX;
            _minZ = minZ;
            _cellSizeX = sizeX / resolution;
            _cellSizeZ = sizeZ / resolution;
        }

        public void Clear() => Array.Clear(_cells, 0, _cells.Length);

        public float Get(int column, int row) => _cells[row * Resolution + column];

        public float GetAt(float x, float z) =>
            Get(ToIndex(x, _minX, _cellSizeX), ToIndex(z, _minZ, _cellSizeZ));

        /// <summary>Opens every hidden cell whose centre lies inside the circle.</summary>
        public void Reveal(float x, float z, float radius) => Stamp(x, z, radius, HIDDEN, OPEN);

        /// <summary>Closes every open cell whose centre lies inside the circle.</summary>
        public void Block(float x, float z, float radius) => Stamp(x, z, radius, OPEN, BLOCKED);

        private void Stamp(float x, float z, float radius, float from, float to)
        {
            int minColumn = ToIndex(x - radius, _minX, _cellSizeX);
            int maxColumn = ToIndex(x + radius, _minX, _cellSizeX);
            int minRow = ToIndex(z - radius, _minZ, _cellSizeZ);
            int maxRow = ToIndex(z + radius, _minZ, _cellSizeZ);
            float sqrRadius = radius * radius;
            for (int row = minRow; row <= maxRow; row++)
            {
                float dz = _minZ + (row + 0.5f) * _cellSizeZ - z;
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    float dx = _minX + (column + 0.5f) * _cellSizeX - x;
                    int index = row * Resolution + column;
                    if (dx * dx + dz * dz <= sqrRadius && _cells[index] == from) _cells[index] = to;
                }
            }
        }

        private int ToIndex(float value, float min, float cellSize) =>
            Math.Min(Math.Max((int)Math.Floor((value - min) / cellSize), 0), Resolution - 1);
    }
}
