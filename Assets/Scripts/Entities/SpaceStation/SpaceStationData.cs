using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver, IUnitModelObserver
    {
    }
    [CreateAssetMenu(fileName = nameof(SpaceStationData), menuName = "Data/SpaceStationData")]
    public class SpaceStationData:Data, IModel, ISpaceStationModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [Tooltip("Optional wreck of each faction's station. A faction without one is only removed.")]
        [SerializeField] private DictionaryWrapper<FactionType, UnitWreckData> wrecks;

        [Header("Hangar")]
        [Tooltip("Squadron bay of each faction's station; lost squadrons are replaced from its reserve.")]
        [SerializeField] private DictionaryWrapper<FactionType, HangarBay> hangarBays;
        [field: SerializeField] public float HangarInitialDelay { get; private set; } = 1f;
        [field: SerializeField] public float HangarLaunchInterval { get; private set; } = 8f;

        public HangarBay GetHangarBay(FactionType factionType) => hangarBays.Dictionary[factionType];

        public bool TryGetWreck(FactionType factionType, out UnitWreckData wreck) =>
            wrecks.Dictionary.TryGetValue(factionType, out wreck);
    }
}
