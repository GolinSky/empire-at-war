using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Services.Selection
{
    public sealed class ShipMarqueeHull : IEntityFacade
    {
        private readonly Transform _ship;
        private readonly Bounds _localBounds;
        private readonly Vector3[] _corners = new Vector3[8];

        public ShipMarqueeHull(Transform ship, Bounds localBounds)
        {
            _ship = ship;
            _localBounds = localBounds;
        }

        public bool Overlaps(MarqueeRectangle rectangle, UnityEngine.Camera camera)
        {
            // Only the depth occupied by the root hull collider can intersect the selection.
            Matrix4x4 toCamera = camera.worldToCameraMatrix * _ship.localToWorldMatrix;
            float depth = -toCamera.MultiplyPoint3x4(_localBounds.center).z;
            Vector3 extent = _localBounds.extents;
            float depthRadius = Mathf.Abs(toCamera.m20) * extent.x + Mathf.Abs(toCamera.m21) * extent.y +
                                Mathf.Abs(toCamera.m22) * extent.z;
            float near = Mathf.Max(camera.nearClipPlane, depth - depthRadius);
            float far = Mathf.Min(camera.farClipPlane, depth + depthRadius);
            if (near > far) return false;

            // The near/far rectangles form a frustum. Work in hull-local axes, centered on the box.
            Matrix4x4 worldToLocal = _ship.worldToLocalMatrix;
            for (int i = 0; i < _corners.Length; i++)
            {
                Vector3 screen = new Vector3((i & 1) == 0 ? rectangle.MinX : rectangle.MaxX,
                    (i & 2) == 0 ? rectangle.MinY : rectangle.MaxY, i < 4 ? near : far);
                _corners[i] = worldToLocal.MultiplyPoint3x4(camera.ScreenToWorldPoint(screen)) - _localBounds.center;
            }

            // Convex polyhedron SAT: both sets of face normals, then all pairs of edge directions.
            if (Separated(Vector3.right) || Separated(Vector3.up) || Separated(Vector3.forward)) return false;
            Vector3 horizontal = _corners[5] - _corners[4];
            Vector3 vertical = _corners[6] - _corners[4];
            if (Separated(Vector3.Cross(horizontal, vertical)) ||
                SeparatedAgainstBoxEdges(horizontal) || SeparatedAgainstBoxEdges(vertical)) return false;

            for (int i = 0; i < 4; i++)
            {
                Vector3 edge = _corners[i + 4] - _corners[i];
                if (SeparatedAgainstBoxEdges(edge)) return false;
            }

            return !Separated(Vector3.Cross(horizontal, _corners[4] - _corners[0])) &&
                   !Separated(Vector3.Cross(horizontal, _corners[6] - _corners[2])) &&
                   !Separated(Vector3.Cross(vertical, _corners[4] - _corners[0])) &&
                   !Separated(Vector3.Cross(vertical, _corners[5] - _corners[1]));
        }

        private bool SeparatedAgainstBoxEdges(Vector3 edge) =>
            Separated(Vector3.Cross(edge, Vector3.right)) ||
            Separated(Vector3.Cross(edge, Vector3.up)) ||
            Separated(Vector3.Cross(edge, Vector3.forward));

        private bool Separated(Vector3 axis)
        {
            Vector3 extent = _localBounds.extents;
            float radius = Mathf.Abs(axis.x) * extent.x + Mathf.Abs(axis.y) * extent.y + Mathf.Abs(axis.z) * extent.z;
            float min = Vector3.Dot(axis, _corners[0]);
            float max = min;
            for (int i = 1; i < _corners.Length; i++)
            {
                float projection = Vector3.Dot(axis, _corners[i]);
                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }
            // Projection and inverse projection can round a touching edge just outside the box.
            return (min > radius && !Mathf.Approximately(min, radius)) ||
                   (max < -radius && !Mathf.Approximately(max, -radius));
        }
    }
}
