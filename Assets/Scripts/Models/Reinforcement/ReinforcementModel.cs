using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Reinforcement
{
    public interface IReinforcementModelObserver:IModelObserver
    {
        event Action<bool> OnSpawnShip;
        event Action<ShipType, Sprite> OnReinforcementAdded;
        
        Dictionary<ShipType, Transform> SpawnShips { get; }
        
        ReinforcementDraggable ReinforcementButton { get; }
        Vector3 SpawnShipPosition { get; }
        
        bool IsTrySpawning { get; }
    }
    
    [CreateAssetMenu(fileName = "ReinforcementModel", menuName = "Model/ReinforcementModel")]
    public class ReinforcementModel:Model, IReinforcementModelObserver
    {
        public event Action<bool> OnSpawnShip;
        public event Action<ShipType, Sprite> OnReinforcementAdded;
        [field: SerializeField] public ReinforcementDraggable ReinforcementButton { get; private set; }

        [SerializeField] private DictionaryWrapper<ShipType, Transform> spawnShipWrapper;
        [SerializeField] private FactionsModel factionsModel;
        
        public bool IsTrySpawning { get; set; }
        public Vector3 SpawnShipPosition { get; set; }
        [Inject]
        private SkirmishGameData SkirmishGameData { get; }
        
        public Dictionary<ShipType, Transform> SpawnShips => spawnShipWrapper.Dictionary;

        public void InvokeSpawnShipEvent(bool success)
        {
            OnSpawnShip?.Invoke(success);
        }

        public void AddReinforcement(ShipType shipType)
        {
            OnReinforcementAdded?.Invoke(shipType, factionsModel.GetFactionData(SkirmishGameData.PlayerFactionType)[shipType].Icon);
        }
    }
}