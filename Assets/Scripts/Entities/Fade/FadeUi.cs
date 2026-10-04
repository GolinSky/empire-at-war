using System.Threading;
using DG.Tweening;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Fade
{
    /// <summary>Full-screen overlay that hides the scene while it loads or changes.</summary>
    public sealed class FadeUi : BaseUi, IFadeUi
    {
        [SerializeField] private Image fadeImage;

        private Tween _fadeTween;

        public void Cover()
        {
            KillTween();
            ShowOnTop();
            SetAlpha(1f);
        }

        public Awaitable FadeInAsync(float duration, CancellationToken cancellationToken)
        {
            ShowOnTop();
            SetAlpha(0f);
            return FadeAsync(1f, duration, false, cancellationToken);
        }

        public Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken)
        {
            ShowOnTop();
            SetAlpha(1f);
            return FadeAsync(0f, duration, true, cancellationToken);
        }

        private Awaitable FadeAsync(float alpha, float duration, bool hideOnComplete,
            CancellationToken cancellationToken)
        {
            KillTween();
            AwaitableCompletionSource completion = new AwaitableCompletionSource();
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                KillTween();
                completion.TrySetCanceled();
            });
            // Unscaled, so the fade plays at the same speed whatever the battle time scale is.
            _fadeTween = fadeImage.DOFade(alpha, duration).SetUpdate(true).OnComplete(() =>
            {
                _fadeTween = null;
                registration.Dispose();
                if (hideOnComplete)
                {
                    Hide();
                }

                completion.TrySetResult();
            });
            return completion.Awaitable;
        }

        // Later popups must not draw over the fade.
        private void ShowOnTop()
        {
            transform.SetAsLastSibling();
            Show();
        }

        private void SetAlpha(float alpha)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }

        private void KillTween()
        {
            if (_fadeTween != null)
            {
                _fadeTween.Kill();
                _fadeTween = null;
            }
        }

        private void OnDestroy()
        {
            KillTween();
        }
    }
}
