using DG.Tweening;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUi : BaseUi, IUnitOrderFeedbackUi
    {
        private const float ATTACK_FADE_IN = 0.09f;
        private const float ATTACK_SETTLE = 0.22f;
        private const float ATTACK_FADE_START = 0.30f;
        private const float ATTACK_FADE_OUT = 0.28f;
        private const float WAVE_DURATION = 0.68f;
        private const float ECHO_DELAY = 0.12f;

        [SerializeField] private RectTransform screenRect;
        [SerializeField] private RectTransform attackAnchor;
        [SerializeField] private RectTransform movementAnchor;
        [SerializeField] private Image attackImage;
        [SerializeField] private Image movementWave;
        [SerializeField] private Image movementEcho;

        private IUnitOrderFeedbackPresenter _presenter;
        private Sequence _attackSequence;
        private Sequence _movementSequence;
        private bool _isDisposed;

        public void SetPresenter(IUnitOrderFeedbackPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            attackAnchor.gameObject.SetActive(false);
            movementAnchor.gameObject.SetActive(false);
        }

        public void PlayAttack(Vector2 screenPosition)
        {
            StopAttack();
            SetPosition(attackAnchor, screenPosition);
            attackAnchor.gameObject.SetActive(true);
            SetAlpha(attackImage, 0f);
            attackImage.rectTransform.localScale = Vector3.one * 1.3f;
            _attackSequence = DOTween.Sequence()
                .Append(attackImage.DOFade(1f, ATTACK_FADE_IN).SetEase(Ease.OutQuad))
                .Insert(0f, attackImage.rectTransform.DOScale(1f, ATTACK_SETTLE).SetEase(Ease.OutCubic))
                .Insert(ATTACK_FADE_START, attackImage.DOFade(0f, ATTACK_FADE_OUT).SetEase(Ease.InQuad))
                .Insert(ATTACK_FADE_START, attackImage.rectTransform.DOScale(1.08f, ATTACK_FADE_OUT)
                    .SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _attackSequence = null;
                    attackAnchor.gameObject.SetActive(false);
                    _presenter.AttackFeedbackCompleted();
                });
        }

        public void PlayMovement(Vector2 screenPosition)
        {
            StopMovement();
            SetPosition(movementAnchor, screenPosition);
            movementAnchor.gameObject.SetActive(true);
            SetAlpha(movementWave, 0f);
            SetAlpha(movementEcho, 0f);
            movementWave.rectTransform.localScale = Vector3.one * 0.28f;
            movementEcho.rectTransform.localScale = Vector3.one * 0.28f;
            _movementSequence = DOTween.Sequence();
            AddWave(_movementSequence, movementWave, 0f, 0.9f);
            AddWave(_movementSequence, movementEcho, ECHO_DELAY, 0.5f);
            _movementSequence.SetUpdate(true).OnComplete(() =>
            {
                _movementSequence = null;
                movementAnchor.gameObject.SetActive(false);
            });
        }

        public void SetAttackPosition(Vector2 screenPosition) => SetPosition(attackAnchor, screenPosition);

        public void StopAttack()
        {
            if (_attackSequence != null)
            {
                _attackSequence.Kill();
                _attackSequence = null;
            }
            attackAnchor.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            StopAttack();
            StopMovement();
        }

        private void OnDestroy() => Dispose();

        private void StopMovement()
        {
            if (_movementSequence != null)
            {
                _movementSequence.Kill();
                _movementSequence = null;
            }
            movementAnchor.gameObject.SetActive(false);
        }

        private void SetPosition(RectTransform anchor, Vector2 screenPosition)
        {
            // The owning DefaultCanvas is Screen Space Overlay.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenRect, screenPosition, null, out Vector2 position);
            anchor.anchoredPosition = position;
        }

        private static void AddWave(Sequence sequence, Image image, float delay, float opacity)
        {
            sequence.Insert(delay, image.rectTransform.DOScale(1.35f, WAVE_DURATION).SetEase(Ease.OutQuad));
            sequence.Insert(delay, image.DOFade(opacity, 0.08f).SetEase(Ease.OutQuad));
            sequence.Insert(delay + 0.12f, image.DOFade(0f, WAVE_DURATION - 0.12f).SetEase(Ease.InQuad));
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
