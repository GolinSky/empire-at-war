using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipRequests
    {
        private readonly ITooltipService _tooltipService;

        private readonly Dictionary<object, TooltipHandle> _handles = new Dictionary<object, TooltipHandle>();

        public TooltipRequests(ITooltipService tooltipService) => _tooltipService = tooltipService;

        public void Show(object source, object key, TooltipAnchor anchor, Func<bool> isValid, Func<TooltipContent> build)
        {
            _handles[source] = _tooltipService.Show(new TooltipContentProvider(source, key, isValid, build), anchor);
        }

        public void Hide(object source)
        {
            if (!_handles.TryGetValue(source, out TooltipHandle handle)) return;
            _tooltipService.Hide(handle);
            _handles.Remove(source);
        }

        public void HideAll()
        {
            foreach (TooltipHandle handle in _handles.Values) _tooltipService.Hide(handle);
            _handles.Clear();
        }
    }
}
