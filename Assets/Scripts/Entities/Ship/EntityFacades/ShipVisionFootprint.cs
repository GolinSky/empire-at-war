using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipVisionFootprint : IVisionFootprintFacade
    {
        private readonly Transform _transform;
        private readonly IReadOnlyList<Vector2> _localHull;
        private readonly Vector2[] _worldHull;
        private Matrix4x4 _lastMatrix;
        private Rect _worldBounds;

        public ShipVisionFootprint(Transform transform, long entityId, IReadOnlyList<Vector2> localHull)
        {
            _transform = transform;
            EntityId = entityId;
            _localHull = localHull;
            _worldHull = new Vector2[localHull.Count];
        }

        public long EntityId { get; }

        public IReadOnlyList<Vector2> WorldHull
        {
            get
            {
                RefreshWorldHull();
                return _worldHull;
            }
        }

        public Rect WorldBounds
        {
            get
            {
                RefreshWorldHull();
                return _worldBounds;
            }
        }

        private void RefreshWorldHull()
        {
            if (_worldHull.Length < 3)
                throw new InvalidOperationException($"Ship {EntityId} has no baked vision hull.");

            Matrix4x4 matrix = _transform.localToWorldMatrix;
            if (matrix.Equals(_lastMatrix)) return;
            _lastMatrix = matrix;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < _localHull.Count; i++)
            {
                Vector2 local = _localHull[i];
                Vector3 world = matrix.MultiplyPoint3x4(new Vector3(local.x, 0f, local.y));
                Vector2 point = new Vector2(world.x, world.z);
                _worldHull[i] = point;
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            _worldBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
