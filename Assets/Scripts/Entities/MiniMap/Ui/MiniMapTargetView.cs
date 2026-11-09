using UnityEngine;

namespace EmpireAtWar.Views.MiniMap
{
    /// <summary>Pulsing ring on the minimap that marks a point other systems point the player to (tutorial targets).</summary>
    public sealed class MiniMapTargetView : MonoBehaviour
    {
        private const float PULSE_SPEED = 4f;
        private const float PULSE_SCALE = 0.35f;

        [SerializeField] private RectTransform ring;

        public void Show(Vector2 miniMapPosition)
        {
            ring.anchoredPosition = miniMapPosition;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update() =>
            ring.localScale = Vector3.one * (1f + PULSE_SCALE * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PULSE_SPEED)));
    }
}
