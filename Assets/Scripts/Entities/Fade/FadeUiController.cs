using System.Threading;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Fade
{
    public sealed class FadeUiController : UiController, ILateDisposable
    {
        private FadeUi _ui;

        public FadeUiController(IUiService uiService, IUiCancelRouter cancelRouter)
            : base(uiService, cancelRouter) { }

        public void Cover() => GetUi().Cover();

        public Awaitable FadeInAsync(float duration, CancellationToken cancellationToken)
            => GetUi().FadeInAsync(duration, cancellationToken);

        public Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken)
            => GetUi().FadeOutAsync(duration, cancellationToken);

        public void LateDispose()
        {
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }

        // The first request can arrive during another system's Initialize.
        private FadeUi GetUi()
        {
            if (_ui == null) _ui = CreateUi<FadeUi>();
            return _ui;
        }
    }
}
