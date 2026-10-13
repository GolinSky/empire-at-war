using System.Threading;
using DG.Tweening;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Fade
{
    /// <summary>Full-screen overlay that hides the scene while it loads or changes.</summary>
    public sealed class FadeUi : BaseUi
    {
        [SerializeField] private Image fadeImage;

        private Tween _fadeTween;
        private AwaitableCompletionSource _fadeCompletion;
        private CancellationTokenRegistration _fadeCancellation;

        public void Cover()
        {
            KillTween();
            ShowOnTop();
            SetAlpha(1f);
        }

        public Awaitable FadeInAsync(float duration, CancellationToken cancellationToken)
        {
            return FadeAsync(1f, duration, false, cancellationToken);
        }

        public Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken)
        {
            return FadeAsync(0f, duration, true, cancellationToken);
        }

        private Awaitable FadeAsync(float alpha, float duration, bool hideOnComplete,
            CancellationToken cancellationToken)
        {
            KillTween();
            AwaitableCompletionSource completion = new AwaitableCompletionSource();
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled();
                return completion.Awaitable;
            }

            ShowOnTop();
            SetAlpha(1f - alpha);
            _fadeCompletion = completion;
            // Unscaled, so the fade plays at the same speed whatever the battle time scale is.
            Tween tween = fadeImage.DOFade(alpha, duration).SetUpdate(true).OnComplete(() =>
            {
                if (_fadeCompletion != completion) return;
                _fadeTween = null;
                _fadeCompletion = null;
                _fadeCancellation.Dispose();
                _fadeCancellation = default;
                if (hideOnComplete)
                {
                    Hide();
                }

                completion.TrySetResult();
            }).OnKill(() =>
            {
                if (_fadeCompletion == completion)
                {
                    _fadeTween = null;
                    _fadeCompletion = null;
                    _fadeCancellation.Dispose();
                    _fadeCancellation = default;
                }
                completion.TrySetCanceled();
            });
            _fadeTween = tween;
            _fadeCancellation = cancellationToken.Register(() =>
            {
                if (_fadeCompletion == completion) KillTween();
            });
            if (cancellationToken.IsCancellationRequested) _fadeCancellation.Dispose();
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
            Tween tween = _fadeTween;
            AwaitableCompletionSource completion = _fadeCompletion;
            _fadeTween = null;
            _fadeCompletion = null;
            _fadeCancellation.Dispose();
            _fadeCancellation = default;
            if (tween != null) tween.Kill();
            // DOTween can defer OnKill until its next update; lifecycle completion is immediate.
            if (completion != null) completion.TrySetCanceled();
        }

        public void Dispose() => KillTween();

        private void OnDestroy() => Dispose();
    }
}
