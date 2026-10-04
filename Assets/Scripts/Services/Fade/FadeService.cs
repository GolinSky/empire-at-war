using System.Threading;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Services.Fade
{
    public sealed class FadeService : IFadeService
    {
        private readonly IUiService _uiService;

        private IFadeUi _fadeUi;

        public FadeService(IUiService uiService)
        {
            _uiService = uiService;
        }

        public void Cover()
        {
            GetFadeUi().Cover();
        }

        public Awaitable FadeInAsync(float duration, CancellationToken cancellationToken)
        {
            return GetFadeUi().FadeInAsync(duration, cancellationToken);
        }

        public Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken)
        {
            return GetFadeUi().FadeOutAsync(duration, cancellationToken);
        }

        // Created on first use: the first request can come from another system's Initialize.
        private IFadeUi GetFadeUi()
        {
            if (_fadeUi == null)
            {
                _fadeUi = (IFadeUi)_uiService.CreateUi(UiType.Fade, _uiService.PopupCanvasTransform);
            }

            return _fadeUi;
        }
    }
}
