using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    /// <summary>
    /// One pooled minimap ping built from the tutorial target ring: pulses, then fades out and deactivates.
    /// Combat pings also show a semi-transparent disc. Fading uses a CanvasGroup, so no mesh is rebuilt.
    /// </summary>
    public sealed class MiniMapAlertView : MonoBehaviour
    {
        private const float PULSE_SPEED = 6f;
        private const float PULSE_SCALE = 0.35f;
        private const float FADE_PORTION = 0.4f;

        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform ring;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Graphic[] ringGraphics;
        [SerializeField] private Graphic disc;

        private float _duration;
        private float _elapsed;

        public bool IsPlaying => gameObject.activeSelf;
        public float Elapsed => _elapsed;

        public void Play(Vector2 anchoredPosition, Color color, float discAlpha, float duration)
        {
            root.anchoredPosition = anchoredPosition;
            root.SetAsLastSibling();
            for (int i = 0; i < ringGraphics.Length; i++) ringGraphics[i].color = color;
            disc.enabled = discAlpha > 0f;
            disc.color = new Color(color.r, color.g, color.b, discAlpha);
            _duration = duration;
            _elapsed = 0f;
            canvasGroup.alpha = 1f;
            ring.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void Stop() => gameObject.SetActive(false);

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= _duration)
            {
                Stop();
                return;
            }

            ring.localScale = Vector3.one * (1f + PULSE_SCALE * (0.5f + 0.5f * Mathf.Sin(_elapsed * PULSE_SPEED)));
            float fadeStart = _duration * (1f - FADE_PORTION);
            canvasGroup.alpha = _elapsed <= fadeStart ? 1f : 1f - (_elapsed - fadeStart) / (_duration - fadeStart);
        }
    }
}
