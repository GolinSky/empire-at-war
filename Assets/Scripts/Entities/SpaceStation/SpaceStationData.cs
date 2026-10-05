using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.SpaceStation
{
    [CreateAssetMenu(fileName = nameof(SpaceStationData), menuName = "Data/SpaceStationData")]
    public class SpaceStationData:Data
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [Tooltip("Optional wreck of each faction's station. A faction without one is only removed.")]
        [SerializeField] private DictionaryWrapper<FactionType, UnitWreckData> wrecks;

        [Header("Hangar")]
        [Tooltip("Squadron bay of each faction's station; lost squadrons are replaced from its reserve.")]
        [SerializeField] private DictionaryWrapper<FactionType, HangarBay> hangarBays;
        [field: SerializeField] public float HangarInitialDelay { get; private set; } = 1f;
        [field: SerializeField] public float HangarLaunchInterval { get; private set; } = 8f;

        [Header("Upgrades")]
        [Tooltip("Durability per station level; element 0 is level 1. Hardpoints set their own unlock level.")]
        [SerializeField] private StationLevelStats[] levelStats;

        public HangarBay GetHangarBay(FactionType factionType) => hangarBays.Dictionary[factionType];

        public StationLevelStats GetLevelStats(int level) => levelStats[level - 1];

        public bool TryGetWreck(FactionType factionType, out UnitWreckData wreck) =>
            wrecks.Dictionary.TryGetValue(factionType, out wreck);
    }
}
