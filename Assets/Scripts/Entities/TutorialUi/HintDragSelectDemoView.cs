using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>Loops a drag-select gesture: a mouse with the left button held draws a box over three ships.</summary>
    public sealed class HintDragSelectDemoView : MonoBehaviour
    {
        private const float WAIT_SECONDS = 0.35f;
        private const float DRAG_SECONDS = 1.1f;
        private const float HOLD_SECONDS = 0.7f;
        private const float LOOP_SECONDS = WAIT_SECONDS + DRAG_SECONDS + HOLD_SECONDS;

        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform selectionBox;
        [SerializeField] private RectTransform cursor;
        [SerializeField] private Image[] ships;
        [SerializeField] private Color idleShipColor = new Color(0.58f, 0.64f, 0.72f, 1f);
        [SerializeField] private Color selectedShipColor = new Color(1f, 0.78f, 0.24f, 1f);

        private float _time;

        public void Play()
        {
            _time = 0f;
            gameObject.SetActive(true);
        }

        public void Stop() => gameObject.SetActive(false);

        private void Update()
        {
            _time = (_time + Time.unscaledDeltaTime) % LOOP_SECONDS;
            float drag = Mathf.Clamp01((_time - WAIT_SECONDS) / DRAG_SECONDS);
            float eased = drag * drag * (3f - 2f * drag);
            Rect bounds = area.rect;
            Vector2 start = new Vector2(bounds.xMin + bounds.width * 0.12f, bounds.yMax - bounds.height * 0.15f);
            Vector2 end = new Vector2(bounds.xMax - bounds.width * 0.12f, bounds.yMin + bounds.height * 0.15f);
            Vector2 current = Vector2.Lerp(start, end, eased);
            cursor.anchoredPosition = current;
            bool isDragging = _time >= WAIT_SECONDS;
            selectionBox.gameObject.SetActive(isDragging);
            Vector2 min = Vector2.Min(start, current);
            Vector2 max = Vector2.Max(start, current);
            selectionBox.anchoredPosition = (min + max) * 0.5f;
            selectionBox.sizeDelta = max - min;
            foreach (Image ship in ships)
            {
                Vector2 position = ((RectTransform)ship.transform).anchoredPosition;
                bool isInside = isDragging && position.x >= min.x && position.x <= max.x &&
                                position.y >= min.y && position.y <= max.y;
                ship.color = isInside ? selectedShipColor : idleShipColor;
            }
        }
    }
}
