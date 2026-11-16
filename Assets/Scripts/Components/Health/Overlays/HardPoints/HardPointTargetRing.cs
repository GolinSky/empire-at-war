using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>A hollow selection ring with dark edges for contrast against the battle.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HardPointTargetRing : MaskableGraphic
    {
        private const int SEGMENTS = 48;
        private const float BAND_WIDTH = 4f;
        private const float BORDER_WIDTH = 1f;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = rectTransform.rect;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            Color border = new Color(0.03f, 0.04f, 0.06f, color.a);
            AddBand(vertices, rect.center, outer - BAND_WIDTH, outer - BAND_WIDTH + BORDER_WIDTH, border);
            AddBand(vertices, rect.center, outer - BAND_WIDTH + BORDER_WIDTH, outer - BORDER_WIDTH, color);
            AddBand(vertices, rect.center, outer - BORDER_WIDTH, outer, border);
        }

        private static void AddBand(VertexHelper vertices, Vector2 center, float inner, float outer, Color tint)
        {
            for (int segment = 0; segment < SEGMENTS; segment++)
            {
                float angle = segment * Mathf.PI * 2f / SEGMENTS;
                float nextAngle = (segment + 1) * Mathf.PI * 2f / SEGMENTS;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 nextDirection = new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle));
                int index = vertices.currentVertCount;
                vertices.AddVert(center + direction * inner, tint, Vector2.zero);
                vertices.AddVert(center + direction * outer, tint, Vector2.zero);
                vertices.AddVert(center + nextDirection * outer, tint, Vector2.zero);
                vertices.AddVert(center + nextDirection * inner, tint, Vector2.zero);
                vertices.AddTriangle(index, index + 2, index + 1);
                vertices.AddTriangle(index, index + 3, index + 2);
            }
        }
    }
}
