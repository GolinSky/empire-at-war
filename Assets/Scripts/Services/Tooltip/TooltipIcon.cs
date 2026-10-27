using System.Collections.Generic;

namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipIcon
    {
        public IReadOnlyList<string> IconKeys { get; }
        public string Label { get; }

        public TooltipIcon(IReadOnlyList<string> iconKeys, string label) { IconKeys = iconKeys; Label = label; }
    }
}
