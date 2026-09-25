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
        [SerializeField] private Sprite asteroidIcon;

        private IReadOnlyList<MiniMapObstacle> _obstacles;
        private Vector2Range _mapRange;

        public override Texture mainTexture => asteroidIcon.texture;

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
                vertices.AddVert(center + new Vector2(-radius.x, -radius.y), color, new Vector2(0f, 0f));
                vertices.AddVert(center + new Vector2(-radius.x, radius.y), color, new Vector2(0f, 1f));
                vertices.AddVert(center + new Vector2(radius.x, radius.y), color, new Vector2(1f, 1f));
                vertices.AddVert(center + new Vector2(radius.x, -radius.y), color, new Vector2(1f, 0f));
                vertices.AddTriangle(first, first + 1, first + 2);
                vertices.AddTriangle(first, first + 2, first + 3);
            }
        }
    }
}
