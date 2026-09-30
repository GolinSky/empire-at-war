using System;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [Serializable]
    public sealed class UnitMatchupEntry
    {
        [SerializeField] private string iconKey;
        [SerializeField] private string label;
        public TooltipIcon Content => new TooltipIcon(iconKey, label);
    }
}
