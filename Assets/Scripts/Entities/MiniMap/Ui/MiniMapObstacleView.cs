using System.Collections.Generic;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MiniMapObstacleView : MaskableGraphic
    {
        private const int ELLIPSE_SEGMENTS = 32;

        private IReadOnlyList<MiniMapObstacle> _obstacles;
        private Vector2Range _mapRange;

        public void SetData(IReadOnlyList<MiniMapObstacle> obstacles, Vector2Range mapRange)
        {
            _obstacles = obstacles;
            _mapRange = mapRange;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (_obstacles == null)
                return;

            Rect rect = rectTransform.rect;
            float scaleX = rect.width / (_mapRange.Max.x - _mapRange.Min.x);
            float scaleY = rect.height / (_mapRange.Max.y - _mapRange.Min.y);
            for (int i = 0; i < _obstacles.Count; i++)
            {
                MiniMapObstacle obstacle = _obstacles[i];
                Vector2 center = new Vector2(
                    rect.xMin + (obstacle.CenterX - _mapRange.Min.x) * scaleX,
                    rect.yMin + (obstacle.CenterZ - _mapRange.Min.y) * scaleY);
                Vector2 radius = new Vector2(obstacle.RadiusX * scaleX, obstacle.RadiusZ * scaleY);

                int first = vertices.currentVertCount;
                vertices.AddVert(center, color, Vector2.zero);
                for (int segment = 0; segment < ELLIPSE_SEGMENTS; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / ELLIPSE_SEGMENTS;
                    vertices.AddVert(
                        center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y),
                        color,
                        Vector2.zero);
                }

                for (int segment = 0; segment < ELLIPSE_SEGMENTS; segment++)
                {
                    vertices.AddTriangle(
                        first,
                        first + 1 + segment,
                        first + 1 + (segment + 1) % ELLIPSE_SEGMENTS);
                }
            }
        }
    }
}
