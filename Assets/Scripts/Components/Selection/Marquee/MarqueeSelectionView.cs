using UnityEngine;

namespace EmpireAtWar.Components.Selection.Marquee
{
    public interface IMarqueeSelectionView
    {
        void Show(MarqueeRectangle rectangle);
        void Hide();
    }

    public sealed class MarqueeSelectionView : MonoBehaviour, IMarqueeSelectionView
    {
        private const float BORDER_WIDTH = 2f;

        [SerializeField] private Color fillColor = new Color(0.12f, 0.72f, 1f, 0.18f);
        [SerializeField] private Color borderColor = new Color(0.12f, 0.72f, 1f, 0.9f);

        private MarqueeRectangle _rectangle;
        private bool _isVisible;

        public void Show(MarqueeRectangle rectangle)
        {
            _rectangle = rectangle;
            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            Rect rect = new Rect(
                _rectangle.MinX,
                Screen.height - _rectangle.MaxY,
                _rectangle.Width,
                _rectangle.Height);

            Draw(rect, fillColor);
            Draw(new Rect(rect.xMin, rect.yMin, rect.width, BORDER_WIDTH), borderColor);
            Draw(new Rect(rect.xMin, rect.yMax - BORDER_WIDTH, rect.width, BORDER_WIDTH), borderColor);
            Draw(new Rect(rect.xMin, rect.yMin, BORDER_WIDTH, rect.height), borderColor);
            Draw(new Rect(rect.xMax - BORDER_WIDTH, rect.yMin, BORDER_WIDTH, rect.height), borderColor);
        }

        private static void Draw(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
