using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>Station upgrade costs, shared by every faction.</summary>
    [CreateAssetMenu(fileName = nameof(StationLevelData), menuName = "Data/Factions/StationLevelData")]
    public class StationLevelData : Data
    {
        [SerializeField] private FactionData[] levels;
        [Tooltip("Purchase of a second station; it is built at level 1.")]
        [SerializeField] private FactionData secondStation;
        [Tooltip("Second station upgrade costs, one per level like levels.")]
        [SerializeField] private FactionData[] secondStationLevels;

        [field: SerializeField] public int MaxLevel { get; private set; }

        /// <summary>Cost of upgrading from <paramref name="level"/> to the next one; null at max level.</summary>
        public FactionData GetLevelFactionData(int level)
        {
            if (level >= MaxLevel) return null;

            return levels[level - 1];
        }

        public FactionData SecondStation => secondStation;

        /// <summary>Cost of upgrading the second station from <paramref name="level"/>; null at max level.</summary>
        public FactionData GetSecondStationLevelFactionData(int level)
        {
            if (level >= MaxLevel) return null;

            return secondStationLevels[level - 1];
        }
    }
}
