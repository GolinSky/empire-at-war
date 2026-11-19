using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    public sealed class HudButtonAttention : MonoBehaviour
    {
        private const float PULSE_DURATION = 0.9f;

        [SerializeField] private Button button;
        [SerializeField] private GameObject readyIndicator;

        private Tween _pulse;
        private Color _originalColor;
        private bool _shouldPulse;

        public void SetReady(bool ready, bool pulse)
        {
            if (readyIndicator.activeSelf != ready) readyIndicator.SetActive(ready);
            _shouldPulse = ready && pulse;
            if (_shouldPulse) StartPulse();
            else StopPulse();
        }

        private void OnEnable() => StartPulse();

        private void OnDisable() => StopPulse();

        private void StartPulse()
        {
            if (!_shouldPulse || _pulse != null || !isActiveAndEnabled || !Application.isPlaying) return;

            Graphic graphic = button.targetGraphic;
            _originalColor = graphic.color;
            Color readyColor = new(1f, 128f / 255f, 128f / 255f, _originalColor.a);
            _pulse = graphic.DOColor(readyColor, PULSE_DURATION)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void StopPulse()
        {
            if (_pulse == null) return;

            _pulse.Kill();
            _pulse = null;
            button.targetGraphic.color = _originalColor;
        }
    }
}
