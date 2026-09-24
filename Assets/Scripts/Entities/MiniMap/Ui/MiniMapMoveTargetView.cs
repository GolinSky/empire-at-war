using DG.Tweening;
using MPUIKIT;
using UnityEngine;

namespace EmpireAtWar.Views.MiniMap
{
    public sealed class MiniMapMoveTargetView : MonoBehaviour
    {
        private const float START_SCALE = 2.2f;
        private const float SHRINK_DURATION = 0.25f;
        private const float HOLD_DURATION = 0.35f;
        private const float FADE_DURATION = 0.4f;

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private MPImage ringImage;

        private Sequence _sequence;

        private void Awake()
        {
            ringImage.raycastTarget = false;
            ringImage.canvasRenderer.SetAlpha(0f);
        }

        private void OnDestroy()
        {
            _sequence.Kill();
        }

        public void Play(Vector2 anchoredPosition)
        {
            _sequence.Kill();
            rectTransform.SetAsLastSibling();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one * START_SCALE;
            ringImage.canvasRenderer.SetAlpha(1f);

            _sequence = DOTween.Sequence()
                .Append(rectTransform.DOScale(1f, SHRINK_DURATION).SetEase(Ease.OutQuad))
                .AppendInterval(HOLD_DURATION)
                .Append(DOTween.To(
                    () => ringImage.canvasRenderer.GetAlpha(),
                    alpha => ringImage.canvasRenderer.SetAlpha(alpha),
                    0f,
                    FADE_DURATION));
        }
    }
}
