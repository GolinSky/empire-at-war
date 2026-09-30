using System;

namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipContentProvider : ITooltipContentProvider
    {
        private readonly Func<bool> _isValid;
        private readonly Func<TooltipContent> _build;
        public TooltipContentProvider(object source, object key, Func<bool> isValid, Func<TooltipContent> build)
        { Source = source; Key = key; _isValid = isValid; _build = build; }
        public object Source { get; }
        public object Key { get; }
        public bool IsValid => _isValid();
        public TooltipContent Build() => _build();
    }
}
