using System;
using System.Collections.Generic;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Square raster of the map marking which cells belong to an asteroid field.</summary>
    public sealed class FieldGrid
    {
        private static readonly (int Column, int Row)[] EDGE_NEIGHBOURS = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        // Diagonal steps make the edge distance a Chebyshev distance, never longer than the true one.
        private static readonly (int Column, int Row)[] ALL_NEIGHBOURS =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)
        };
        private readonly bool[] _isField;
        private readonly bool[] _visited;
        private readonly Queue<int> _queue = new Queue<int>();

        private readonly Vector2 _min;

        public int Columns { get; }
        public int Rows { get; }
        public float CellSize { get; }
        public int Count => _isField.Length;

        public FieldGrid(Vector2Range bounds, float cellSize)
        {
            _min = bounds.Min;
            Vector2 size = bounds.Max - bounds.Min;
            // Whole cells across the map keep cell centers point-symmetric about the map center.
            Columns = Mathf.Max(1, Mathf.RoundToInt(size.x / cellSize));
            Rows = Mathf.Max(1, Mathf.RoundToInt(size.y / cellSize));
            CellSize = size.x / Columns;
            _isField = new bool[Columns * Rows];
            _visited = new bool[_isField.Length];
        }

        public void SetField(int index, bool isField)
        {
            _isField[index] = isField;
        }

        public Vector3 GetCenter(int index)
        {
            return new Vector3(
                _min.x + (index % Columns + 0.5f) * CellSize,
                0f,
                _min.y + (index / Columns + 0.5f) * CellSize);
        }

        public int GetIndex(Vector3 position)
        {
            int column = Mathf.Clamp(Mathf.FloorToInt((position.x - _min.x) / CellSize), 0, Columns - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt((position.z - _min.y) / CellSize), 0, Rows - 1);
            return row * Columns + column;
        }

        /// <summary>Open cells that no edge-connected open path links to any of <paramref name="starts"/>.</summary>
        public List<int> FindSealedOpenCells(IEnumerable<int> starts)
        {
            Array.Clear(_visited, 0, Count);
            _queue.Clear();
            foreach (int start in starts)
            {
                if (_isField[start] || _visited[start]) continue;
                _visited[start] = true;
                _queue.Enqueue(start);
            }

            while (_queue.Count > 0)
            {
                int index = _queue.Dequeue();
                foreach (int neighbour in GetNeighbours(index, EDGE_NEIGHBOURS))
                {
                    if (_isField[neighbour] || _visited[neighbour]) continue;
                    _visited[neighbour] = true;
                    _queue.Enqueue(neighbour);
                }
            }

            List<int> sealedCells = new List<int>();
            for (int index = 0; index < Count; index++)
                if (!_isField[index] && !_visited[index]) sealedCells.Add(index);
            return sealedCells;
        }

        /// <summary>Groups field cells into edge-connected fields.</summary>
        public List<List<int>> FindFields()
        {
            List<List<int>> fields = new List<List<int>>();
            Array.Clear(_visited, 0, Count);
            _queue.Clear();
            for (int start = 0; start < Count; start++)
            {
                if (!_isField[start] || _visited[start])
                {
                    continue;
                }

                List<int> field = new List<int>();
                _visited[start] = true;
                _queue.Enqueue(start);
                while (_queue.Count > 0)
                {
                    int index = _queue.Dequeue();
                    field.Add(index);
                    foreach (int neighbour in GetNeighbours(index, EDGE_NEIGHBOURS))
                    {
                        if (_isField[neighbour] && !_visited[neighbour])
                        {
                            _visited[neighbour] = true;
                            _queue.Enqueue(neighbour);
                        }
                    }
                }

                fields.Add(field);
            }

            return fields;
        }

        /// <summary>Steps from every field cell to the nearest open cell; the map border counts as open.</summary>
        public int[] GetEdgeDistances()
        {
            int[] distances = new int[Count];
            _queue.Clear();
            for (int index = 0; index < Count; index++)
            {
                int column = index % Columns;
                int row = index / Columns;
                bool isEdge = column == 0 || row == 0 || column == Columns - 1 || row == Rows - 1;
                if (!_isField[index] || isEdge)
                {
                    distances[index] = _isField[index] ? 1 : 0;
                    _queue.Enqueue(index);
                }
                else
                {
                    distances[index] = int.MaxValue;
                }
            }

            while (_queue.Count > 0)
            {
                int index = _queue.Dequeue();
                foreach (int neighbour in GetNeighbours(index, ALL_NEIGHBOURS))
                {
                    if (distances[neighbour] > distances[index] + 1)
                    {
                        distances[neighbour] = distances[index] + 1;
                    _queue.Enqueue(neighbour);
                    }
                }
            }

            return distances;
        }

        private IEnumerable<int> GetNeighbours(int index, (int Column, int Row)[] steps)
        {
            int column = index % Columns;
            int row = index / Columns;
            foreach ((int columnStep, int rowStep) in steps)
            {
                int nextColumn = column + columnStep;
                int nextRow = row + rowStep;
                if (nextColumn >= 0 && nextColumn < Columns && nextRow >= 0 && nextRow < Rows)
                {
                    yield return nextRow * Columns + nextColumn;
                }
            }
        }
    }
}
