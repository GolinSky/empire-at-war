using EmpireAtWar.Entities.Reinforcement;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Reinforcement
{
    // Placement previews are Addressable references, not prefab references, so loading this data does not pull every
    // faction's ship art into its bundle. Keys are Addressables GUIDs for IAssetService.LoadComponent<UnitSpawnView>.
    [CreateAssetMenu(fileName = nameof(ReinforcementData), menuName = "Data/Reinforcement Data")]
    public class ReinforcementData : Data
    {
        [SerializeField] private DictionaryWrapper<ShipType, AssetReferenceGameObject> spawnShipWrapper;
        [SerializeField] private DictionaryWrapper<SquadronType, AssetReferenceGameObject> spawnSquadronWrapper;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, AssetReferenceGameObject> spawnFacilityWrapper;
        [SerializeField] private DictionaryWrapper<DefendPlatformType, AssetReferenceGameObject> defendPlatformWrapper;

        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }
        [field: SerializeField] public int MaxUnitCapacity { get; private set; }

        public string GetSpawnPrefabKey(ShipType shipType)
        {
            return spawnShipWrapper.Dictionary[shipType].AssetGUID;
        }

        public string GetSpawnPrefabKey(MiningFacilityType miningFacilityType)
        {
            if (spawnFacilityWrapper.Dictionary.TryGetValue(miningFacilityType, out AssetReferenceGameObject spawnView))
            {
                return spawnView.AssetGUID;
            }

            return spawnFacilityWrapper.Dictionary.Values.First().AssetGUID;
        }

        public string GetSpawnPrefabKey(SquadronType squadronType)
        {
            return spawnSquadronWrapper.Dictionary[squadronType].AssetGUID;
        }

        public string GetSpawnPrefabKey(DefendPlatformType defendPlatformType)
        {
            if (defendPlatformWrapper.Dictionary.TryGetValue(defendPlatformType, out AssetReferenceGameObject spawnView))
            {
                return spawnView.AssetGUID;
            }

            return defendPlatformWrapper.Dictionary.Values.First().AssetGUID;
        }

        /// <summary>Previews a player of this faction can place: its ships and squadrons plus every structure.</summary>
        public IEnumerable<string> GetPreviewKeys(FactionDefinition faction)
        {
            foreach (ShipType ship in faction.Ships.Keys)
            {
                if (spawnShipWrapper.Dictionary.TryGetValue(ship, out AssetReferenceGameObject preview))
                    yield return preview.AssetGUID;
            }
            foreach (SquadronType squadron in faction.Squadrons.Keys)
            {
                if (spawnSquadronWrapper.Dictionary.TryGetValue(squadron, out AssetReferenceGameObject preview))
                    yield return preview.AssetGUID;
            }
            foreach (AssetReferenceGameObject preview in spawnFacilityWrapper.Dictionary.Values)
                yield return preview.AssetGUID;
            foreach (AssetReferenceGameObject preview in defendPlatformWrapper.Dictionary.Values)
                yield return preview.AssetGUID;
        }
    }
}
