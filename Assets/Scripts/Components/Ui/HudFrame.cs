using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudFrame : MaskableGraphic
    {
        private const int CORNER_SEGMENTS = 8;
        private const int POINT_COUNT = 4 * (CORNER_SEGMENTS + 1);

        [SerializeField] private Color borderColor = new Color32(40, 70, 87, 255);

        [FormerlySerializedAs("bevel")]
        [SerializeField] private float cornerRadius = 10f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Polygon(mesh, rect, radius, color);
            Ring(mesh, rect, radius, borderColor);
        }

        private static Rect Inset(Rect rect, float amount) =>
            new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);

        private static void Ring(VertexHelper mesh, Rect rect, float corner, Color tint)
        {
            float width = Mathf.Min(1f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Rect inner = Inset(rect, width);
            int start = mesh.currentVertCount;
            for (int i = 0; i < POINT_COUNT; i++)
            {
                mesh.AddVert(Point(rect, corner, i), tint, Vector2.zero);
                mesh.AddVert(Point(inner, Mathf.Max(0f, corner - width), i), tint, Vector2.zero);
            }
            for (int i = 0; i < POINT_COUNT; i++)
            {
                int current = start + i * 2;
                int next = start + (i + 1) % POINT_COUNT * 2;
                mesh.AddTriangle(current, next, current + 1);
                mesh.AddTriangle(next, next + 1, current + 1);
            }
        }

        private static Vector3 Point(Rect rect, float radius, int index)
        {
            int corner = index / (CORNER_SEGMENTS + 1);
            float angle = (90f - corner * 90f - index % (CORNER_SEGMENTS + 1) * 90f / CORNER_SEGMENTS) * Mathf.Deg2Rad;
            float x = corner < 2 ? rect.xMax - radius : rect.xMin + radius;
            float y = corner == 0 || corner == 3 ? rect.yMax - radius : rect.yMin + radius;
            return new Vector3(x + Mathf.Cos(angle) * radius, y + Mathf.Sin(angle) * radius);
        }

        private static void Polygon(VertexHelper mesh, Rect rect, float corner, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.center.x, rect.center.y), tint, Vector2.zero);
            for (int i = 0; i < POINT_COUNT; i++)
                mesh.AddVert(Point(rect, corner, i), tint, Vector2.zero);
            for (int i = 0; i < POINT_COUNT; i++)
                mesh.AddTriangle(start, start + i + 1, start + (i + 1) % POINT_COUNT + 1);
        }
    }
}
