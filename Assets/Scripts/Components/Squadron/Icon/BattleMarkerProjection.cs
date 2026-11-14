using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    internal sealed class BattleMarkerProjection
    {
        private const float CELL_PITCH = 84f;
        private const float MIN_CELL_PITCH = 42f;
        private const float CENTER_RESPONSE = 12f;
        private const int LOCAL_PLACEMENT_RINGS = 5;

        private readonly Dictionary<long, int> _groupIndices = new Dictionary<long, int>();
        private readonly Dictionary<(long, Vector2Int), int> _partitionIndices =
            new Dictionary<(long, Vector2Int), int>();
        private readonly List<Grid> _groups = new List<Grid>();
        private readonly List<Grid> _partitions = new List<Grid>();
        private readonly List<int> _placementOrder = new List<int>();
        private readonly List<Rect> _occupied = new List<Rect>();
        private readonly Dictionary<(long, Vector2Int), Vector2> _previousOffsets =
            new Dictionary<(long, Vector2Int), Vector2>();
        private readonly List<(long, Vector2Int)> _expiredOffsets = new List<(long, Vector2Int)>();
        private readonly Comparison<int> _comparePartitions;
        private Matrix4x4 _worldToClip;
        private Matrix4x4 _worldToCamera;
        private Rect _viewport;
        private Rect _usable;

        public BattleMarkerProjection() => _comparePartitions = ComparePartitions;

        public void Draw(IReadOnlyList<IBattleMarker> markers, SquadronIconGrouping grouping,
            Camera camera, float zoom, float deltaTime)
        {
            _worldToCamera = camera.worldToCameraMatrix;
            _worldToClip = camera.projectionMatrix * _worldToCamera;
            _viewport = camera.pixelRect;
            Rect safeArea = Screen.safeArea;
            _usable = Rect.MinMaxRect(Mathf.Max(_viewport.xMin, safeArea.xMin),
                Mathf.Max(_viewport.yMin, safeArea.yMin), Mathf.Min(_viewport.xMax, safeArea.xMax),
                Mathf.Min(_viewport.yMax, safeArea.yMax));
            float centerBlend = 1f - Mathf.Exp(-CENTER_RESPONSE * deltaTime);
            float blend = 1f - Mathf.Exp(-CENTER_RESPONSE * Time.unscaledDeltaTime);
            _occupied.Clear();
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (!marker.IsVisible)
                {
                    marker.Hide();
                    continue;
                }

                if (!marker.IsSquadron)
                {
                    DrawStandalone(marker, zoom, blend);
                    if (marker.IsShown) _occupied.Add(marker.ScreenRect);
                    continue;
                }

                if (grouping.TryGetSlot(marker.EntityId, out long id, out Vector2Int cell))
                    AccumulateGroup(id, cell, marker.Position);
            }

            float pitch = CELL_PITCH * zoom;
            PrepareGroups(pitch, zoom, centerBlend);
            PlacePartitions(markers, grouping, zoom);
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (!marker.IsVisible || !marker.IsSquadron) continue;
                if (!grouping.TryGetSlot(marker.EntityId, out long id, out Vector2Int cell))
                {
                    DrawStandalone(marker, zoom, blend);
                    continue;
                }

                Grid group = _groups[_groupIndices[id]];
                if (group.Count < 2)
                {
                    DrawStandalone(marker, zoom, blend);
                    continue;
                }

                Grid partition = _partitions[_partitionIndices[(id, PartitionFor(group, cell))]];
                if (partition.OnScreen)
                {
                    Vector2 center = partition.Screen + ((Vector2)cell - partition.CellCenter) * group.Pitch;
                    marker.Display(center, _viewport, zoom, group.Pitch, blend, id, cell, group.Pitch / pitch);
                }
                else
                {
                    marker.Hide();
                }
            }
            ResetGroups();
        }

        private void AccumulateGroup(long id, Vector2Int cell, Vector3 position)
        {
            if (!_groupIndices.TryGetValue(id, out int index))
            {
                index = _groups.Count;
                _groupIndices.Add(id, index);
                _groups.Add(new Grid { Id = id });
            }

            Grid group = _groups[index];
            group.AddCell(cell);
            group.Sum += position;
            _groups[index] = group;
        }

        private void PrepareGroups(float pitch, float zoom, float blend)
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                Grid group = _groups[i];
                if (group.Count < 2) continue;
                Vector3 center = group.Sum / group.Count;
                group.Center = group.WasActive ? Vector3.Lerp(group.Center, center, blend) : center;
                group.OnScreen = Project(group.Center, out group.Screen);
                group.Screen.y += 60f * zoom;
                Vector2Int dimensions = group.Max - group.Min + Vector2Int.one;
                float fitPitch = Mathf.Min(_usable.width / dimensions.x, _usable.height / dimensions.y);
                group.Pitch = Mathf.Min(pitch, Mathf.Max(MIN_CELL_PITCH, fitPitch));
                group.CellsPerPartition = new Vector2Int(
                    Mathf.Max(1, Mathf.FloorToInt(_usable.width / group.Pitch)),
                    Mathf.Max(1, Mathf.FloorToInt(_usable.height / group.Pitch)));
                if (dimensions.x > group.CellsPerPartition.x)
                    group.CellsPerPartition.x = Mathf.CeilToInt((float)dimensions.x /
                        Mathf.CeilToInt((float)dimensions.x / group.CellsPerPartition.x));
                if (dimensions.y > group.CellsPerPartition.y)
                    group.CellsPerPartition.y = Mathf.CeilToInt((float)dimensions.y /
                        Mathf.CeilToInt((float)dimensions.y / group.CellsPerPartition.y));
                _groups[i] = group;
            }
        }

        private void PlacePartitions(IReadOnlyList<IBattleMarker> markers, SquadronIconGrouping grouping, float zoom)
        {
            _partitions.Clear();
            _partitionIndices.Clear();
            _placementOrder.Clear();
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (!marker.IsVisible || !marker.IsSquadron ||
                    !grouping.TryGetSlot(marker.EntityId, out long id, out Vector2Int cell))
                    continue;
                Grid group = _groups[_groupIndices[id]];
                if (group.Count < 2) continue;

                Vector2Int coordinates = PartitionFor(group, cell);
                var key = (id, coordinates);
                if (!_partitionIndices.TryGetValue(key, out int index))
                {
                    index = _partitions.Count;
                    _partitionIndices.Add(key, index);
                    _placementOrder.Add(index);
                    _partitions.Add(new Grid { Id = id, Partition = coordinates });
                }

                Grid partition = _partitions[index];
                partition.AddCell(cell);
                _partitions[index] = partition;
            }

            _placementOrder.Sort(_comparePartitions);
            for (int i = 0; i < _placementOrder.Count; i++)
            {
                int index = _placementOrder[i];
                Grid partition = _partitions[index];
                Grid group = _groups[_groupIndices[partition.Id]];
                Vector2 halfSize = (Vector2)(partition.Max - partition.Min + Vector2Int.one) * (group.Pitch * 0.5f);
                Vector2 desired = group.Screen + (partition.CellCenter - group.CellCenter) * group.Pitch;
                // Oversize grids are tiled in readable partitions. Each partition can use available screen space.
                Vector2 groupSize = (Vector2)(group.Max - group.Min + Vector2Int.one) * group.Pitch;
                Rect worldBounds = new Rect(group.Screen - groupSize * 0.5f, groupSize);
                partition.OnScreen = group.OnScreen && worldBounds.Overlaps(_viewport);
                if (partition.OnScreen)
                {
                    Vector2 clamped = ClampCenter(desired, halfSize);
                    var key = (partition.Id, partition.Partition);
                    _previousOffsets.TryGetValue(key, out Vector2 previousOffset);
                    partition.Screen = FindPlacement(clamped, halfSize, previousOffset, group.Pitch);
                    _previousOffsets[key] = partition.Screen - clamped;
                    _occupied.Add(new Rect(partition.Screen - halfSize, halfSize * 2f));
                }
                _partitions[index] = partition;
            }

            foreach (var key in _previousOffsets.Keys)
                if (!_partitionIndices.ContainsKey(key)) _expiredOffsets.Add(key);
            for (int i = 0; i < _expiredOffsets.Count; i++) _previousOffsets.Remove(_expiredOffsets[i]);
            _expiredOffsets.Clear();
        }

        private Vector2 FindPlacement(Vector2 center, Vector2 halfSize, Vector2 previousOffset, float pitch)
        {
            Vector2 previous = ClampCenter(center + previousOffset, halfSize);
            if (IsFree(previous, halfSize)) return previous;
            if (IsFree(center, halfSize)) return center;

            // Try the nearest edge-aligned moves as well as surrounding cells. The bounded search
            // lets transient congestion overlap instead of making projected markers vanish.
            Vector2 best = center;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _occupied.Count; i++)
            {
                Rect obstacle = _occupied[i];
                if (!obstacle.Overlaps(new Rect(center - halfSize, 2f * halfSize))) continue;
                Consider(new Vector2(obstacle.xMin - halfSize.x, center.y), center, halfSize, ref best, ref bestDistance);
                Consider(new Vector2(obstacle.xMax + halfSize.x, center.y), center, halfSize, ref best, ref bestDistance);
                Consider(new Vector2(center.x, obstacle.yMin - halfSize.y), center, halfSize, ref best, ref bestDistance);
                Consider(new Vector2(center.x, obstacle.yMax + halfSize.y), center, halfSize, ref best, ref bestDistance);
            }

            for (int ring = 1; ring <= LOCAL_PLACEMENT_RINGS && ring * pitch < bestDistance; ring++)
            {
                for (int step = -ring; step < ring; step++)
                {
                    Consider(center + new Vector2(step, ring) * pitch, center, halfSize, ref best, ref bestDistance);
                    Consider(center + new Vector2(ring, -step) * pitch, center, halfSize, ref best, ref bestDistance);
                    Consider(center + new Vector2(-step, -ring) * pitch, center, halfSize, ref best, ref bestDistance);
                    Consider(center + new Vector2(-ring, step) * pitch, center, halfSize, ref best, ref bestDistance);
                }
            }

            // Search the viewport only after the local ring failed. Oversized partitions can need
            // a larger move than five cells even though there is room elsewhere on screen.
            if (float.IsPositiveInfinity(bestDistance))
            {
                for (float y = _usable.yMin + halfSize.y; y < _usable.yMax - halfSize.y + pitch; y += pitch)
                for (float x = _usable.xMin + halfSize.x; x < _usable.xMax - halfSize.x + pitch; x += pitch)
                    Consider(new Vector2(x, y), center, halfSize, ref best, ref bestDistance);
            }

            return best;
        }

        private void Consider(Vector2 candidate, Vector2 center, Vector2 halfSize, ref Vector2 best, ref float distance)
        {
            candidate = ClampCenter(candidate, halfSize);
            float nextDistance = Vector2.Distance(candidate, center);
            if (nextDistance >= distance || !IsFree(candidate, halfSize)) return;
            best = candidate;
            distance = nextDistance;
        }

        private Vector2 ClampCenter(Vector2 center, Vector2 halfSize) => new Vector2(
            Mathf.Clamp(center.x, Mathf.Min(_usable.center.x, _usable.xMin + halfSize.x),
                Mathf.Max(_usable.center.x, _usable.xMax - halfSize.x)),
            Mathf.Clamp(center.y, Mathf.Min(_usable.center.y, _usable.yMin + halfSize.y),
                Mathf.Max(_usable.center.y, _usable.yMax - halfSize.y)));

        private bool IsFree(Vector2 center, Vector2 halfSize)
        {
            Rect bounds = new Rect(center - halfSize, halfSize * 2f);
            for (int i = 0; i < _occupied.Count; i++)
                if (_occupied[i].Overlaps(bounds)) return false;
            return true;
        }

        private void DrawStandalone(BattleMarker marker, float zoom, float blend)
        {
            if (Project(marker.Position, out Vector2 center))
            {
                center.y += marker.ScreenOffset * zoom;
                marker.Display(center, _viewport, zoom, marker.ClickSize * zoom, blend);
            }
            else
            {
                marker.Hide();
            }
        }

        private bool Project(Vector3 position, out Vector2 screen)
        {
            Vector4 clip = _worldToClip * new Vector4(position.x, position.y, position.z, 1f);
            if (clip.w <= 0f || _worldToCamera.MultiplyPoint3x4(position).z >= 0f)
            {
                screen = default;
                return false;
            }
            screen = _viewport.min + new Vector2((clip.x / clip.w * 0.5f + 0.5f) * _viewport.width,
                (clip.y / clip.w * 0.5f + 0.5f) * _viewport.height);
            return true;
        }

        private void ResetGroups()
        {
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                Grid group = _groups[i];
                if (group.Count > 0)
                {
                    group.WasActive = group.Count >= 2;
                    group.Count = 0;
                    group.Sum = Vector3.zero;
                    _groups[i] = group;
                }
                else
                {
                    _groupIndices.Remove(group.Id);
                    int last = _groups.Count - 1;
                    if (i != last)
                    {
                        _groups[i] = _groups[last];
                        _groupIndices[_groups[i].Id] = i;
                    }
                    _groups.RemoveAt(last);
                }
            }
        }

        private static Vector2Int PartitionFor(Grid group, Vector2Int cell) =>
            new Vector2Int((cell.x - group.Min.x) / group.CellsPerPartition.x,
                (cell.y - group.Min.y) / group.CellsPerPartition.y);

        private int ComparePartitions(int left, int right)
        {
            Grid a = _partitions[left];
            Grid b = _partitions[right];
            int order = a.Id.CompareTo(b.Id);
            if (order != 0) return order;
            order = a.Partition.y.CompareTo(b.Partition.y);
            return order != 0 ? order : a.Partition.x.CompareTo(b.Partition.x);
        }

        private struct Grid
        {
            public long Id;
            public bool WasActive;
            public bool OnScreen;
            public int Count;
            public Vector2Int Min;
            public Vector2Int Max;
            public Vector2Int Partition;
            public Vector2Int CellsPerPartition;
            public float Pitch;
            public Vector3 Sum;
            public Vector3 Center;
            public Vector2 Screen;
            public Vector2 CellCenter => ((Vector2)Min + Max) * 0.5f;

            public void AddCell(Vector2Int cell)
            {
                Min = Count == 0 ? cell : Vector2Int.Min(Min, cell);
                Max = Count == 0 ? cell : Vector2Int.Max(Max, cell);
                Count++;
            }
        }
    }
}
