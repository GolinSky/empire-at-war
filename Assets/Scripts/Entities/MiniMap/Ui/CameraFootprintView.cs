using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CameraFootprintView : MaskableGraphic
    {
        private const float OUTLINE_WIDTH = 1.25f;
        private const float FILL_ALPHA = 0.08f;

        private CameraMarkData _model;
        private Vector2Range _mapRange;
        private bool _initialized;

        public void SetData(CameraMarkData model, Vector2Range mapRange)
        {
            _model = model;
            _mapRange = mapRange;
            _initialized = true;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            if (_initialized)
                SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (!_initialized || _model.Vertices.Count < 3)
                return;

            Color fill = color;
            fill.a *= FILL_ALPHA;
            int count = _model.Vertices.Count;
            for (int i = 0; i < count; i++)
                vertices.AddVert(GetPosition(i), fill, Vector2.zero);

            for (int i = 1; i < count - 1; i++)
                vertices.AddTriangle(0, i, i + 1);

            for (int i = 0; i < count; i++)
            {
                Vector2 start = GetPosition(i);
                Vector2 end = GetPosition((i + 1) % count);
                Vector2 edge = end - start;
                Vector2 inset = new Vector2(-edge.y, edge.x).normalized * OUTLINE_WIDTH;
                int first = vertices.currentVertCount;
                vertices.AddVert(start, color, Vector2.zero);
                vertices.AddVert(end, color, Vector2.zero);
                vertices.AddVert(end + inset, color, Vector2.zero);
                vertices.AddVert(start + inset, color, Vector2.zero);
                vertices.AddTriangle(first, first + 1, first + 2);
                vertices.AddTriangle(first + 2, first + 3, first);
            }
        }

        private Vector2 GetPosition(int index)
        {
            var point = _model.Vertices[index];
            Rect rect = rectTransform.rect;
            return new Vector2(
                rect.xMin + (point.X - _mapRange.Min.x) / (_mapRange.Max.x - _mapRange.Min.x) * rect.width,
                rect.yMin + (point.Z - _mapRange.Min.y) / (_mapRange.Max.y - _mapRange.Min.y) * rect.height);
        }
    }
}
