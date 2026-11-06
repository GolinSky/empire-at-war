using UnityEngine;

namespace EmpireAtWar.Services.Tooltip
{
    public interface IWorldTooltipSource
    {
        bool TryGetTooltip(Ray ray, out ITooltipContentProvider provider);
    }
}
