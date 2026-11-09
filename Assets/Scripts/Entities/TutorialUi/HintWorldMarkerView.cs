using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Marks a world point for a hint: a pulsing ring with a bobbing chevron above it. Off screen only an arrow
    /// at the screen edge shows, pointing toward the target.
    /// </summary>
    public sealed class HintWorldMarkerView : MonoBehaviour
    {
        private const float EDGE_MARGIN = 64f;
        private const float PULSE_SPEED = 4f;
        private const float PULSE_SCALE = 0.18f;
        private const float BOB_SPEED = 5f;
        private const float BOB_HEIGHT = 10f;

        [SerializeField] private RectTransform marker;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform ring;
        [SerializeField] private RectTransform chevron;
        [SerializeField] private RectTransform edgeArrow;

        private Vector2 _chevronRest;

        private void Awake() => _chevronRest = chevron.anchoredPosition;

        /// <param name="screenPoint">Projected world point; z below zero means behind the camera.</param>
        /// <param name="canvasRoot">Full-screen rect the marker lives in (Screen Space Overlay).</param>
        public void Show(Vector3 screenPoint, RectTransform canvasRoot)
        {
            gameObject.SetActive(true);
            Vector2 point = screenPoint;
            if (screenPoint.z < 0f) point = new Vector2(Screen.width, Screen.height) - point;
            Rect inner = new Rect(EDGE_MARGIN, EDGE_MARGIN, Screen.width - EDGE_MARGIN * 2f,
                Screen.height - EDGE_MARGIN * 2f);
            bool isOnScreen = screenPoint.z >= 0f && inner.Contains(point);
            Vector2 clamped = new Vector2(Mathf.Clamp(point.x, inner.xMin, inner.xMax),
                Mathf.Clamp(point.y, inner.yMin, inner.yMax));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, clamped, null, out Vector2 local);
            marker.anchoredPosition = local;
            // Off screen only the arrow shows: a ring at the screen edge would read as a second target.
            ring.gameObject.SetActive(isOnScreen);
            chevron.gameObject.SetActive(isOnScreen);
            edgeArrow.gameObject.SetActive(!isOnScreen);
            if (isOnScreen) return;
            Vector2 direction = point - new Vector2(Screen.width, Screen.height) * 0.5f;
            edgeArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            float wave = Mathf.Sin(Time.unscaledTime * PULSE_SPEED);
            ring.localScale = Vector3.one * (1f + PULSE_SCALE * (0.5f + 0.5f * wave));
            group.alpha = 0.7f + 0.3f * wave;
            chevron.anchoredPosition = _chevronRest +
                                       Vector2.up * (BOB_HEIGHT * Mathf.Abs(Mathf.Sin(Time.unscaledTime * BOB_SPEED)));
        }
    }
}
