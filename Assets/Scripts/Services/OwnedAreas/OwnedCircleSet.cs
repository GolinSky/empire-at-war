using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.OwnedAreas
{
    /// <summary>Circles keyed by their transform. Registering a transform again updates its owner and radius.</summary>
    public sealed class OwnedCircleSet
    {
        private readonly List<OwnedCircle> _circles = new List<OwnedCircle>();
        // Per frame, each queried (team, allied) filter's circles as (x, z, radius): hundreds of queries per frame
        // otherwise read every circle's transform and resolve its team relation.
        private readonly Dictionary<(PlayerId Team, bool Allied), List<Vector3>> _snapshots =
            new Dictionary<(PlayerId Team, bool Allied), List<Vector3>>();
        private readonly HashSet<(PlayerId Team, bool Allied)> _builtSnapshots = new HashSet<(PlayerId Team, bool Allied)>();
        private int _snapshotFrame = -1;

        public IReadOnlyList<OwnedCircle> Circles => _circles;

        public void Register(PlayerId owner, Transform source, float radius)
        {
            _snapshotFrame = -1;
            int index = IndexOf(source);
            if (index >= 0)
            {
                _circles[index].Owner = owner;
                _circles[index].Radius = radius;
                return;
            }

            _circles.Add(new OwnedCircle(owner, source, radius));
        }

        public void Unregister(Transform source)
        {
            _snapshotFrame = -1;
            int index = IndexOf(source);
            if (index >= 0)
            {
                _circles.RemoveAt(index);
            }
        }

        /// <summary>True when a circle whose owner is (or, with <paramref name="allied"/> false, is not)
        /// allied with <paramref name="team"/> reaches within <paramref name="margin"/> of the position.</summary>
        public bool AnyContains(Vector3 position, float margin, IPlayerRelations relations, PlayerId team, bool allied)
        {
            List<Vector3> circles = GetSnapshot(relations, team, allied);
            for (int i = 0; i < circles.Count; i++)
            {
                Vector3 circle = circles[i];
                float dx = position.x - circle.x;
                float dz = position.z - circle.y;
                float radius = circle.z + margin;
                if (dx * dx + dz * dz <= radius * radius) return true;
            }

            return false;
        }

        private List<Vector3> GetSnapshot(IPlayerRelations relations, PlayerId team, bool allied)
        {
            if (_snapshotFrame != Time.frameCount)
            {
                _snapshotFrame = Time.frameCount;
                _builtSnapshots.Clear();
            }

            if (!_snapshots.TryGetValue((team, allied), out List<Vector3> circles))
            {
                circles = new List<Vector3>();
                _snapshots.Add((team, allied), circles);
            }

            if (!_builtSnapshots.Add((team, allied))) return circles;
            circles.Clear();
            for (int i = 0; i < _circles.Count; i++)
            {
                OwnedCircle circle = _circles[i];
                if (relations.IsAllied(circle.Owner, team) != allied) continue;
                Vector3 center = circle.Transform.position;
                circles.Add(new Vector3(center.x, center.z, circle.Radius));
            }

            return circles;
        }

        private int IndexOf(Transform source)
        {
            for (int i = 0; i < _circles.Count; i++)
            {
                if (_circles[i].Transform == source) return i;
            }

            return -1;
        }
    }
}
