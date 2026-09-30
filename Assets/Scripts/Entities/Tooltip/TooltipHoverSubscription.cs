using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipHoverSubscription : IDisposable
    {
        private readonly TooltipHoverView _view;
        private readonly Action<object, TooltipAnchor, object> _started;
        private readonly TooltipRequests _requests;
        public TooltipHoverSubscription(TooltipHoverView view,
            Action<object, TooltipAnchor, object> started, TooltipRequests requests)
        {
            _view = view;
            _started = started;
            _requests = requests;
            _view.HoverStarted += _started;
            _view.HoverEnded += _requests.Hide;
        }
        public void Dispose()
        {
            _view.HoverStarted -= _started;
            _view.HoverEnded -= _requests.Hide;
            _requests.HideAll();
        }
    }
}
