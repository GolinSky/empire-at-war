using System;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    /// <summary>One faction's station wrecks, one per station level, so a station wrecks as the level it fell at.</summary>
    [Serializable]
    public sealed class StationLevelWrecks
    {
        [Tooltip("Element 0 is the level 1 wreck, through element 4 for level 5.")]
        [SerializeField] private UnitWreckData[] levels;

        public UnitWreckData[] Levels => levels;

        public UnitWreckData Get(int level) => levels[level - 1];
    }
}
