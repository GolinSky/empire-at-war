using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MiniMapHazardView : MaskableGraphic
    {
        private const float OUTLINE_WIDTH = 1f;
        private const float SAMPLE_PIXELS = 2f;
        private IReadOnlyList<MiniMapHazard> _hazards;
        private Vector2Range _mapRange;
        private readonly List<RectTransform> _icons = new List<RectTransform>();

        public void SetData(IReadOnlyList<MiniMapHazard> hazards, Vector2Range range)
        {
            _hazards = hazards;
            _mapRange = range;
            foreach (RectTransform icon in _icons) Destroy(icon.gameObject);
            _icons.Clear();
            foreach (MiniMapHazard hazard in hazards)
            {
                var iconObject = new GameObject("HazardIcon", typeof(RectTransform));
                var icon = iconObject.AddComponent<Image>();
                icon.rectTransform.SetParent(transform, false);
                icon.rectTransform.sizeDelta = new Vector2(14f, 14f);
                icon.sprite = hazard.Icon;
                icon.color = hazard.Color;
                icon.raycastTarget = false;
                _icons.Add(icon.rectTransform);
            }
            RefreshIcons();
            SetVerticesDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_hazards != null) RefreshIcons();
        }

        private void RefreshIcons()
        {
            for (int i = 0; i < _icons.Count; i++)
                _icons[i].anchoredPosition = ToLocal(_hazards[i].CenterX, _hazards[i].CenterZ);
        }

        private Vector2 ToLocal(float x, float z)
        {
            Rect rect = rectTransform.rect;
            return new Vector2(rect.xMin + (x - _mapRange.Min.x) / (_mapRange.Max.x - _mapRange.Min.x) * rect.width,
                rect.yMin + (z - _mapRange.Min.y) / (_mapRange.Max.y - _mapRange.Min.y) * rect.height);
        }

        // Rows of inside grid samples become quads, so the fill follows the same outline as gameplay.
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (_hazards == null) return;
            Rect rect = rectTransform.rect;
            float metresPerPixel = (_mapRange.Max.x - _mapRange.Min.x) / Mathf.Max(1f, rect.width);
            foreach (MiniMapHazard hazard in _hazards)
            {
                HazardShape shape = hazard.Shape;
                int step = Mathf.Max(1, Mathf.FloorToInt(metresPerPixel * SAMPLE_PIXELS / shape.CellSize));
                float edgeDepth = metresPerPixel * OUTLINE_WIDTH;
                Color fill = hazard.Color;
                fill.a = 0.25f;
                Color edge = hazard.Color;
                edge.a = 0.75f;
                AddRuns(vertices, shape, step, fill, distance => distance <= 0f);
                AddRuns(vertices, shape, step, edge, distance => distance <= 0f && distance > -edgeDepth);
            }
        }

        private void AddRuns(VertexHelper vertices, HazardShape shape, int step, Color color, System.Func<float, bool> include)
        {
            float half = shape.CellSize * step * 0.5f;
            for (int row = 0; row < shape.Rows; row += step)
            {
                int start = -1;
                for (int column = 0; ; column += step)
                {
                    bool end = column >= shape.Columns;
                    bool inside = !end && include(shape.GetDistance(column, row));
                    if (inside && start < 0) start = column;
                    if (!inside && start >= 0)
                    {
                        Vector3 first = shape.GetSamplePosition(start, row);
                        Vector3 last = shape.GetSamplePosition(column - step, row);
                        AddQuad(vertices, ToLocal(first.x - half, first.z - half), ToLocal(last.x + half, last.z + half), color);
                        start = -1;
                    }
                    if (end) break;
                }
            }
        }

        private static void AddQuad(VertexHelper vertices, Vector2 min, Vector2 max, Color color)
        {
            int index = vertices.currentVertCount;
            vertices.AddVert(new Vector2(min.x, min.y), color, Vector2.zero);
            vertices.AddVert(new Vector2(min.x, max.y), color, Vector2.zero);
            vertices.AddVert(new Vector2(max.x, max.y), color, Vector2.zero);
            vertices.AddVert(new Vector2(max.x, min.y), color, Vector2.zero);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index, index + 2, index + 3);
        }
    }
}
