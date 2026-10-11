using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.OwnedAreas
{
    /// <summary>Circles keyed by their transform. Registering a transform again updates its owner and radius.</summary>
    public sealed class OwnedCircleSet
    {
        private readonly List<OwnedCircle> _circles = new List<OwnedCircle>();
        // XZ centres read once per frame: hundreds of queries per frame otherwise read every circle's transform.
        private readonly List<Vector2> _centers = new List<Vector2>();
        private int _centersFrame = -1;

        public IReadOnlyList<OwnedCircle> Circles => _circles;

        public void Register(PlayerId owner, Transform source, float radius)
        {
            _centersFrame = -1;
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
            _centersFrame = -1;
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
            RefreshCenters();
            for (int i = 0; i < _circles.Count; i++)
            {
                OwnedCircle circle = _circles[i];
                if (relations.IsAllied(circle.Owner, team) != allied) continue;
                float dx = position.x - _centers[i].x;
                float dz = position.z - _centers[i].y;
                float radius = circle.Radius + margin;
                if (dx * dx + dz * dz <= radius * radius) return true;
            }

            return false;
        }

        private void RefreshCenters()
        {
            if (_centersFrame == Time.frameCount) return;
            _centersFrame = Time.frameCount;
            _centers.Clear();
            for (int i = 0; i < _circles.Count; i++)
            {
                Vector3 center = _circles[i].Transform.position;
                _centers.Add(new Vector2(center.x, center.z));
            }
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
