using System;

namespace EmpireAtWar.Services.Tooltip
{
    public interface ITooltipModelObserver
    {
        event Action Shown;
        event Action ContentChanged;
        event Action AnchorChanged;
        event Action Hidden;
        bool IsVisible { get; }
        TooltipContent Content { get; }
        TooltipAnchor Anchor { get; }
    }
}
