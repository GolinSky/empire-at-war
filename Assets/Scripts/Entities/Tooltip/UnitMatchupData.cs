using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [CreateAssetMenu(fileName = nameof(UnitMatchupData), menuName = "Data/Tooltip/Unit Matchups")]
    public sealed class UnitMatchupData : Data
    {
        [SerializeField] private UnitMatchupEntry[] strongAgainst = Array.Empty<UnitMatchupEntry>();
        [SerializeField] private UnitMatchupEntry[] weakAgainst = Array.Empty<UnitMatchupEntry>();
        public IEnumerable<TooltipIcon> StrongAgainst => strongAgainst.Select(entry => entry.Content);
        public IEnumerable<TooltipIcon> WeakAgainst => weakAgainst.Select(entry => entry.Content);
    }
}
