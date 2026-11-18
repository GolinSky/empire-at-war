using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    /// <summary>Blinks a graphic red and back to signal a rejected click.</summary>
    public static class UiRejectBlink
    {
        private const float BLINK_DURATION = 0.12f;
        private const int BLINK_LOOPS = 4;
        private static readonly Color32 REJECT_COLOR = new(232, 110, 108, 255);

        public static void Play(Graphic graphic)
        {
            // Completing a running blink restores the authored colour before it is captured again.
            DOTween.Complete(graphic);
            DOTween.To(() => graphic.color, color => graphic.color = color, (Color)REJECT_COLOR, BLINK_DURATION)
                .SetLoops(BLINK_LOOPS, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetTarget(graphic);
        }
    }
}
