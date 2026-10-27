using System;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [Serializable]
    public sealed class UnitMatchupEntry
    {
        // One icon per opposing faction; a single icon for a named ship or a faction-neutral target.
        [SerializeField] private string[] iconKeys = Array.Empty<string>();
        [SerializeField] private string label;

        public TooltipIcon Content => new TooltipIcon(iconKeys, label);
    }
}
