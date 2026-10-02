using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudFrame : MaskableGraphic
    {
        [SerializeField] private Color borderColor = new Color32(40, 70, 87, 255);
        [SerializeField] private Color accentColor = new Color32(54, 200, 243, 255);
        [SerializeField] private float bevel = 10f;
        [SerializeField] private float accentWidth = 80f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Polygon(mesh, rect, bevel, color);
            Ring(mesh, rect, bevel, borderColor);
            rect = Inset(rect, 4f);
            Ring(mesh, rect, Mathf.Max(0f, bevel - 4f), borderColor * new Color(1f, 1f, 1f, 0.6f));
            Rect edge = GetPixelAdjustedRect();
            Polygon(mesh, new Rect(edge.xMin + bevel, edge.yMax - 2f,
                Mathf.Min(accentWidth, edge.width - bevel * 2f), 2f), 0f, accentColor);
        }

        private static Rect Inset(Rect rect, float amount) =>
            new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);

        private static void Ring(VertexHelper mesh, Rect rect, float corner, Color tint)
        {
            Rect inner = Inset(rect, 1f);
            int start = mesh.currentVertCount;
            for (int i = 0; i < 8; i++)
            {
                mesh.AddVert(Point(rect, corner, i), tint, Vector2.zero);
                mesh.AddVert(Point(inner, Mathf.Max(0f, corner - 1f), i), tint, Vector2.zero);
            }
            for (int i = 0; i < 8; i++)
            {
                int current = start + i * 2;
                int next = start + (i + 1) % 8 * 2;
                mesh.AddTriangle(current, next, current + 1);
                mesh.AddTriangle(next, next + 1, current + 1);
            }
        }

        private static Vector3 Point(Rect rect, float corner, int index)
        {
            float cut = Mathf.Min(corner, Mathf.Min(rect.width, rect.height) * 0.5f);
            return index switch
            {
                0 => new Vector3(rect.xMin + cut, rect.yMax),
                1 => new Vector3(rect.xMax - cut, rect.yMax),
                2 => new Vector3(rect.xMax, rect.yMax - cut),
                3 => new Vector3(rect.xMax, rect.yMin + cut),
                4 => new Vector3(rect.xMax - cut, rect.yMin),
                5 => new Vector3(rect.xMin + cut, rect.yMin),
                6 => new Vector3(rect.xMin, rect.yMin + cut),
                _ => new Vector3(rect.xMin, rect.yMax - cut)
            };
        }

        private static void Polygon(VertexHelper mesh, Rect rect, float corner, Color tint)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            float cut = Mathf.Min(corner, Mathf.Min(rect.width, rect.height) * 0.5f);
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.center.x, rect.center.y), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin + cut, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax - cut, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax - cut), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin + cut), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax - cut, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin + cut, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin + cut), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax - cut), tint, Vector2.zero);
            for (int i = 0; i < 8; i++) mesh.AddTriangle(start, start + i + 1, start + (i + 1) % 8 + 1);
        }
    }
}
