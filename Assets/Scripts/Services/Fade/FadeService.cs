using System.Threading;
using EmpireAtWar.Entities.Fade;
using UnityEngine;

namespace EmpireAtWar.Services.Fade
{
    public sealed class FadeService : IFadeService
    {
        private readonly FadeUiController _controller;

        public FadeService(FadeUiController controller) { _controller = controller; }

        public void Cover() => _controller.Cover();

        public Awaitable FadeInAsync(float duration, CancellationToken cancellationToken)
            => _controller.FadeInAsync(duration, cancellationToken);

        public Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken)
            => _controller.FadeOutAsync(duration, cancellationToken);
    }
}
