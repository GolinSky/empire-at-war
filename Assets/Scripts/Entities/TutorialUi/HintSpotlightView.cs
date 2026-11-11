using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>Dims the HUD around a transparent cutout without drawing over the target.</summary>
    public sealed class HintSpotlightView : MaskableGraphic
    {
        private Rect _cutout;

        public void Show(Rect cutout, bool blockPointerOutside)
        {
            raycastTarget = blockPointerOutside;
            gameObject.SetActive(true);
            if (_cutout == cutout) return;
            _cutout = cutout;
            SetVerticesDirty();
        }

        public void Hide() => gameObject.SetActive(false);

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 point);
            return !_cutout.Contains(point) && base.Raycast(screenPoint, eventCamera);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            float left = Mathf.Clamp(_cutout.xMin, bounds.xMin, bounds.xMax);
            float right = Mathf.Clamp(_cutout.xMax, bounds.xMin, bounds.xMax);
            float bottom = Mathf.Clamp(_cutout.yMin, bounds.yMin, bounds.yMax);
            float top = Mathf.Clamp(_cutout.yMax, bounds.yMin, bounds.yMax);
            AddQuad(mesh, bounds.xMin, bounds.yMin, left, bounds.yMax);
            AddQuad(mesh, right, bounds.yMin, bounds.xMax, bounds.yMax);
            AddQuad(mesh, left, bounds.yMin, right, bottom);
            AddQuad(mesh, left, top, right, bounds.yMax);
        }

        private void AddQuad(VertexHelper mesh, float left, float bottom, float right, float top)
        {
            if (right <= left || top <= bottom) return;
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
