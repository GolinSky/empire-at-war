using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
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
        event Action<bool> OnSpawnShip;
        event Action<ShipType, FactionData> OnReinforcementAdded;
        
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
        public event Action<bool> OnSpawnShip;
        public event Action<ShipType, FactionData> OnReinforcementAdded;
        
        [SerializeField] private DictionaryWrapper<ShipType, UnitSpawnView> spawnShipWrapper;

        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }
        [field: SerializeField] public int MaxUnitCapacity { get; private set; }

        private Dictionary<ShipType, FactionData> shipFactionData = new Dictionary<ShipType, FactionData>();
        
        private int currentUnitCapacity;

        public int CurrentUnitCapacity
        {
            get => currentUnitCapacity;
            set
            {
                currentUnitCapacity = value;
                OnCapacityChanged?.Invoke(currentUnitCapacity);
            }
        }

        public int CapacityLeft => MaxUnitCapacity - CurrentUnitCapacity;

        public bool IsTrySpawning { get; set; }

        
        public Dictionary<ShipType, UnitSpawnView> SpawnShips => spawnShipWrapper.Dictionary;

        public UnitSpawnView GetSpawnPrefab(ShipType shipType)
        {
            if (SpawnShips.TryGetValue(shipType, out UnitSpawnView spawnTransform))
            {
                return spawnTransform;
            }

            return SpawnShips.Values.FirstOrDefault();
        }

        public void InvokeSpawnShipEvent(bool success)
        {
            OnSpawnShip?.Invoke(success);
        }

        public bool CanSpawnUnit(ShipType shipType)
        {
            return shipFactionData[shipType].UnitCapacity <= CapacityLeft;
        }

        public void AddUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity += shipFactionData[shipType].UnitCapacity;
        }
        
        public void RemoveUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity -= shipFactionData[shipType].UnitCapacity;
        }
        
        public void AddReinforcement(ShipUnitRequest shipUnitRequest)
        {
            if (!shipFactionData.ContainsKey(shipUnitRequest.ShipType))
            {
                shipFactionData.Add(shipUnitRequest.ShipType,  shipUnitRequest.FactionData);
            }
            OnReinforcementAdded?.Invoke(shipUnitRequest.ShipType, shipUnitRequest.FactionData);
        }
    }
}