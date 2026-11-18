using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Per-frame marker layout. Camera motion maps anchors to screen exactly; only obstacle avoidance and
    /// membership changes go through springs, so panning never drags an icon.
    /// </summary>
    internal sealed class BattleMarkerProjection
    {
        private const float CELL_PITCH = 84f;
        private const float JOIN_PITCHES = 1.25f;
        private const float LEAVE_PITCHES = 3f;
        private const float PLACEMENT_SMOOTH_TIME = 0.12f;
        private const float HULL_PADDING = 4f;

        private readonly SquadronClusterPlacement _placement = new SquadronClusterPlacement();
        private readonly Dictionary<long, SquadronClusterState> _states = new Dictionary<long, SquadronClusterState>();
        private readonly Stack<SquadronClusterState> _pool = new Stack<SquadronClusterState>();
        private readonly List<SquadronClusterState> _active = new List<SquadronClusterState>();
        private readonly List<SquadronClusterState> _order = new List<SquadronClusterState>();
        private readonly List<long> _stale = new List<long>();
        private readonly System.Comparison<SquadronClusterState> _compare = CompareClusters;
        private Matrix4x4 _worldToClip;
        private Matrix4x4 _worldToCamera;
        private Rect _viewport;
        private int _frame;

        public static float Pitch(float zoom) => CELL_PITCH * zoom;
        public static float JoinPixels(float zoom) => JOIN_PITCHES * Pitch(zoom);
        public static float LeavePixels(float zoom) => LEAVE_PITCHES * Pitch(zoom);

        public void Prepare(UnityEngine.Camera camera)
        {
            _worldToCamera = camera.worldToCameraMatrix;
            _worldToClip = camera.projectionMatrix * _worldToCamera;
            _viewport = camera.pixelRect;
        }

        public bool TryProject(Vector3 position, out Vector2 screen)
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

        public void Draw(IReadOnlyList<IBattleMarker> markers, IReadOnlyList<IBattleHull> hulls,
            SquadronIconGrouping grouping, UnityEngine.Camera camera, float zoom, float deltaTime)
        {
            Prepare(camera);
            _frame++;
            float pitch = Pitch(zoom);
            _placement.Begin(_viewport);
            AddHullObstacles(hulls);
            DrawShips(markers, zoom, deltaTime);
            GatherClusters(markers, grouping, zoom);
            PlaceClusters(pitch, zoom, deltaTime);
            DrawSquadrons(markers, pitch, zoom, deltaTime);
            RemoveStaleClusters();
        }

        private void AddHullObstacles(IReadOnlyList<IBattleHull> hulls)
        {
            for (int i = 0; i < hulls.Count; i++)
            {
                IBattleHull hull = hulls[i];
                if (!hull.IsActive) continue;
                Bounds bounds = hull.LocalBounds;
                Vector3 extents = bounds.extents;
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                bool visible = true;
                for (int corner = 0; corner < 8 && visible; corner++)
                {
                    Vector3 local = bounds.center + new Vector3((corner & 1) == 0 ? -extents.x : extents.x,
                        (corner & 2) == 0 ? -extents.y : extents.y, (corner & 4) == 0 ? -extents.z : extents.z);
                    visible = TryProject(hull.Transform.TransformPoint(local), out Vector2 screen);
                    minX = Mathf.Min(minX, screen.x);
                    maxX = Mathf.Max(maxX, screen.x);
                    minY = Mathf.Min(minY, screen.y);
                    maxY = Mathf.Max(maxY, screen.y);
                }

                if (!visible) continue;
                var rect = Rect.MinMaxRect(minX - HULL_PADDING, minY - HULL_PADDING, maxX + HULL_PADDING,
                    maxY + HULL_PADDING);
                if (rect.Overlaps(_viewport)) _placement.AddObstacle(rect);
            }
        }

        private void DrawShips(IReadOnlyList<IBattleMarker> markers, float zoom, float deltaTime)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (marker.IsSquadron) continue;
                if (!marker.IsVisible || !TryProject(marker.Position, out Vector2 anchor))
                {
                    marker.Hide();
                    continue;
                }

                marker.Display(anchor + new Vector2(0f, marker.ScreenOffset * zoom), _viewport, zoom, 0L, deltaTime);
                if (marker.IsShown) _placement.AddObstacle(marker.ScreenRect);
            }
        }

        private void GatherClusters(IReadOnlyList<IBattleMarker> markers, SquadronIconGrouping grouping, float zoom)
        {
            _active.Clear();
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (!marker.IsSquadron) continue;
                marker.ClusterSlot = -1;
                if (!marker.IsVisible || !TryProject(marker.Position, out Vector2 anchor))
                {
                    marker.Hide();
                    continue;
                }

                if (!grouping.TryGetCluster(marker.EntityId, out long id, out int rank, out int count))
                {
                    // Not clustered yet: a lone squadron is a 1×1 grid.
                    id = -marker.EntityId - 1;
                    rank = 0;
                    count = 1;
                }

                if (!_states.TryGetValue(id, out SquadronClusterState state))
                {
                    state = _pool.Count > 0 ? _pool.Pop() : new SquadronClusterState();
                    state.Id = id;
                    state.IsNew = true;
                    state.Candidate = SquadronClusterPlacement.NO_CANDIDATE;
                    state.Stamp = 0;
                    _states.Add(id, state);
                }

                if (state.Stamp != _frame)
                {
                    state.BeginFrame(_frame, count, marker.ScreenOffset * zoom);
                    state.ActiveIndex = _active.Count;
                    _active.Add(state);
                }

                state.Add(marker, anchor, rank, count);
                marker.ProjectedAnchor = anchor;
                marker.Rank = rank;
                marker.ClusterSlot = state.ActiveIndex;
            }
        }

        private void PlaceClusters(float pitch, float zoom, float deltaTime)
        {
            // Larger clusters claim space first; _active keeps its order because markers index into it.
            _order.Clear();
            _order.AddRange(_active);
            _order.Sort(_compare);
            for (int i = 0; i < _order.Count; i++)
            {
                SquadronClusterState state = _order[i];
                state.Centroid = state.Sum / state.Present;
                state.Size = SquadronClusterLayout.Size(state.Count, pitch);
                Vector2 target = _placement.Place(state.Centroid, state.Size, pitch, state.Rise, zoom,
                    state.Candidate, out state.Candidate);
                if (state.IsNew)
                {
                    state.Placement = new ScreenSpring { Value = target - state.Centroid };
                    state.IsNew = false;
                }
                else
                {
                    state.Placement.Step(target - state.Centroid, PLACEMENT_SMOOTH_TIME, deltaTime);
                }

                state.Center = state.Centroid + state.Placement.Value;
                _placement.AddObstacle(new Rect(state.Center - state.Size * 0.5f, state.Size));
            }

        }

        private void DrawSquadrons(IReadOnlyList<IBattleMarker> markers, float pitch, float zoom, float deltaTime)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = (BattleMarker)markers[i];
                if (!marker.IsSquadron || marker.ClusterSlot < 0) continue;
                SquadronClusterState state = _active[marker.ClusterSlot];
                Vector2 target = state.Center + SquadronClusterLayout.CellOffset(marker.Rank, state.Count, pitch);
                marker.Display(target, _viewport, zoom, state.LayoutKey(marker.Rank), deltaTime);
            }
        }

        private void RemoveStaleClusters()
        {
            _stale.Clear();
            foreach (KeyValuePair<long, SquadronClusterState> pair in _states)
                if (pair.Value.Stamp != _frame) _stale.Add(pair.Key);
            for (int i = 0; i < _stale.Count; i++)
            {
                _pool.Push(_states[_stale[i]]);
                _states.Remove(_stale[i]);
            }
        }

        private static int CompareClusters(SquadronClusterState left, SquadronClusterState right)
        {
            int count = right.Count.CompareTo(left.Count);
            return count != 0 ? count : left.Id.CompareTo(right.Id);
        }
    }
}
