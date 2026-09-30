using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipClock : ITooltipClock { public float DeltaTime => Time.unscaledDeltaTime; }
}
