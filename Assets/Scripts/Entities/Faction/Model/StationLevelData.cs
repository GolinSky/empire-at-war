using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>Station upgrade costs, shared by every faction.</summary>
    [CreateAssetMenu(fileName = nameof(StationLevelData), menuName = "Data/Factions/StationLevelData")]
    public class StationLevelData : Data
    {
        [SerializeField] private FactionData[] levels;

        [field: SerializeField] public int MaxLevel { get; private set; }

        /// <summary>Cost of upgrading from <paramref name="level"/> to the next one; null at max level.</summary>
        public FactionData GetLevelFactionData(int level)
        {
            if (level >= MaxLevel) return null;

            return levels[level - 1];
        }
    }
}
