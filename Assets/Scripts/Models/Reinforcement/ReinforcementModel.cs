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
        event Action<bool> OnSpawnShip;
        event Action<ShipType, Sprite> OnReinforcementAdded;
        
        UnitSpawnView GetSpawnPrefab(ShipType shipType);

        SpawnShipUi ReinforcementButton { get; }
        
        bool IsTrySpawning { get; }
    }
    
    [CreateAssetMenu(fileName = "ReinforcementModel", menuName = "Model/ReinforcementModel")]
    public class ReinforcementModel:Model, IReinforcementModelObserver
    {
        public event Action<bool> OnSpawnShip;
        public event Action<ShipType, Sprite> OnReinforcementAdded;
        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }

        [SerializeField] private DictionaryWrapper<ShipType, UnitSpawnView> spawnShipWrapper;
        
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

        public void AddReinforcement(ShipUnitRequest shipUnitRequest)
        {
            OnReinforcementAdded?.Invoke(shipUnitRequest.ShipType, shipUnitRequest.FactionData.Icon);
        }
    }
}