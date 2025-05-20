using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Views.Reinforcement;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Reinforcement
{
    public interface IReinforcementModelObserver:IModelObserver
    {
        event Action<int> OnCapacityChanged; 
        event Action<bool> OnSpawnUnit;
        event Action<string, FactionData> OnReinforcementAdded;
        bool IsTrySpawning { get; }
        int MaxUnitCapacity { get; }
        int CurrentUnitCapacity { get; }
        int CapacityLeft { get; }
        UnitSpawnView GetSpawnPrefab(ShipType shipType);

        SpawnShipUi ReinforcementButton { get; }
        bool CanSpawnUnit(ShipType shipType);
    }
    
    [CreateAssetMenu(fileName = "ReinforcementModel", menuName = "Model/ReinforcementModel")]
    public class ReinforcementModel:Model, IReinforcementModelObserver
    {
        public event Action<int> OnCapacityChanged;
        public event Action<bool> OnSpawnUnit;
        public event Action<string, FactionData> OnReinforcementAdded;

        
        [SerializeField] private DictionaryWrapper<ShipType, UnitSpawnView> spawnShipWrapper;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, UnitSpawnView> spawnFacilityWrapper;
        [SerializeField] private DictionaryWrapper<DefendPlatformType, UnitSpawnView> defendPlatformWrapper;

        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }
        [field: SerializeField] public int MaxUnitCapacity { get; private set; }

        private Dictionary<ShipType, FactionData> _shipFactionData = new Dictionary<ShipType, FactionData>();
        
        private int _currentUnitCapacity;

        public int CurrentUnitCapacity
        {
            get => _currentUnitCapacity;
            set
            {
                _currentUnitCapacity = value;
                OnCapacityChanged?.Invoke(_currentUnitCapacity);
            }
        }

        public int CapacityLeft => MaxUnitCapacity - CurrentUnitCapacity;

        public bool IsTrySpawning { get; set; }
        

        public UnitSpawnView GetSpawnPrefab(ShipType shipType)
        {
            if (spawnShipWrapper.Dictionary.TryGetValue(shipType, out UnitSpawnView spawnTransform))
            {
                return spawnTransform;
            }

            return spawnShipWrapper.Dictionary.Values.FirstOrDefault();
        }
        
        public UnitSpawnView GetSpawnPrefab(MiningFacilityType miningFacilityType)
        {
            if (spawnFacilityWrapper.Dictionary.TryGetValue(miningFacilityType, out UnitSpawnView spawnTransform))
            {
                return spawnTransform;
            }

            return spawnFacilityWrapper.Dictionary.Values.FirstOrDefault();
        }
        public UnitSpawnView GetSpawnPrefab(DefendPlatformType defendPlatformType)
        {
            if (defendPlatformWrapper.Dictionary.TryGetValue(defendPlatformType, out UnitSpawnView spawnTransform))
            {
                return spawnTransform;
            }

            return spawnFacilityWrapper.Dictionary.Values.FirstOrDefault();
        }

        public void InvokeSpawnShipEvent(bool success)
        {
            OnSpawnUnit?.Invoke(success);
        }

        public bool CanSpawnUnit(ShipType shipType)
        {
            return _shipFactionData[shipType].UnitCapacity <= CapacityLeft;
        }

        public void AddUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity += _shipFactionData[shipType].UnitCapacity;
        }
        
        public void RemoveUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity -= _shipFactionData[shipType].UnitCapacity;
        }
        
        public void UpdateShipData(ShipUnitRequest shipUnitRequest)
        {
            if (!_shipFactionData.ContainsKey(shipUnitRequest.Key))
            {
                _shipFactionData.Add(shipUnitRequest.Key,  shipUnitRequest.FactionData);
            }
        }
        
        public void AddReinforcement(UnitRequest unitRequest)
        {
            OnReinforcementAdded?.Invoke(unitRequest.Id,  unitRequest.FactionData);
        }
    }
}