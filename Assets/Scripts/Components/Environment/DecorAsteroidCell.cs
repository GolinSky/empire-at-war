using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    internal sealed class DecorAsteroidCell
    {
        private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>();
        private readonly List<Bounds> _bounds = new List<Bounds>();
        public Bounds Bounds { get; private set; }
        public Matrix4x4[] Matrices { get; private set; }
        public Bounds[] InstanceBounds { get; private set; }

        public void Add(Matrix4x4 matrix, Bounds localBounds)
        {
            Vector3 x = matrix.MultiplyVector(new Vector3(localBounds.extents.x, 0f, 0f));
            Vector3 y = matrix.MultiplyVector(new Vector3(0f, localBounds.extents.y, 0f));
            Vector3 z = matrix.MultiplyVector(new Vector3(0f, 0f, localBounds.extents.z));
            Vector3 extents = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(localBounds.center), extents * 2f);
            Bounds cellBounds = _matrices.Count == 0 ? bounds : Bounds;
            cellBounds.Encapsulate(bounds);
            Bounds = cellBounds;
            _matrices.Add(matrix);
            _bounds.Add(bounds);
        }

        public void Seal()
        {
            Matrices = _matrices.ToArray();
            InstanceBounds = _bounds.ToArray();
            _matrices.Clear();
            _bounds.Clear();
        }

        public bool IsInside(Plane[] planes)
        {
            Vector3 extents = Bounds.extents;
            foreach (Plane plane in planes)
            {
                Vector3 normal = plane.normal;
                float radius = Mathf.Abs(normal.x) * extents.x + Mathf.Abs(normal.y) * extents.y +
                    Mathf.Abs(normal.z) * extents.z;
                if (plane.GetDistanceToPoint(Bounds.center) < radius) return false;
            }
            return true;
        }
    }
}
