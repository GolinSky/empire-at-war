using System.Linq;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Reinforcement
{
    [CreateAssetMenu(fileName = nameof(ReinforcementData), menuName = "Data/Reinforcement Data")]
    public class ReinforcementData : Data
    {
        [SerializeField] private DictionaryWrapper<ShipType, UnitSpawnView> spawnShipWrapper;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, UnitSpawnView> spawnFacilityWrapper;
        [SerializeField] private DictionaryWrapper<DefendPlatformType, UnitSpawnView> defendPlatformWrapper;

        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }
        [field: SerializeField] public int MaxUnitCapacity { get; private set; }

        public UnitSpawnView GetSpawnPrefab(ShipType shipType)
        {
            if (spawnShipWrapper.Dictionary.TryGetValue(shipType, out UnitSpawnView spawnView))
            {
                return spawnView;
            }

            return spawnShipWrapper.Dictionary.Values.FirstOrDefault();
        }

        public UnitSpawnView GetSpawnPrefab(MiningFacilityType miningFacilityType)
        {
            if (spawnFacilityWrapper.Dictionary.TryGetValue(miningFacilityType, out UnitSpawnView spawnView))
            {
                return spawnView;
            }

            return spawnFacilityWrapper.Dictionary.Values.FirstOrDefault();
        }

        public UnitSpawnView GetSpawnPrefab(DefendPlatformType defendPlatformType)
        {
            if (defendPlatformWrapper.Dictionary.TryGetValue(defendPlatformType, out UnitSpawnView spawnView))
            {
                return spawnView;
            }

            return defendPlatformWrapper.Dictionary.Values.FirstOrDefault();
        }
    }
}
