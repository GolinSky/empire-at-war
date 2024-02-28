using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.Views.Reinforcement;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Reinforcement
{
    public interface IReinforcementModelObserver:IModelObserver
    {
        event Action<bool> OnSpawnShip;
        event Action<ShipType, Sprite> OnReinforcementAdded;
        
        ShipSpawnView GetSpawnPrefab(ShipType shipType);

        SpawnShipUi ReinforcementButton { get; }
        
        bool IsTrySpawning { get; }
    }
    
    [CreateAssetMenu(fileName = "ReinforcementModel", menuName = "Model/ReinforcementModel")]
    public class ReinforcementModel:Model, IReinforcementModelObserver
    {
        public event Action<bool> OnSpawnShip;
        public event Action<ShipType, Sprite> OnReinforcementAdded;
        [field: SerializeField] public SpawnShipUi ReinforcementButton { get; private set; }

        [SerializeField] private DictionaryWrapper<ShipType, ShipSpawnView> spawnShipWrapper;
        [SerializeField] private FactionsModel factionsModel;
        
        [Inject(Id = PlayerType.Player)] 
        public FactionType FactionType { get; }
        public bool IsTrySpawning { get; set; }
        
        
        public Dictionary<ShipType, ShipSpawnView> SpawnShips => spawnShipWrapper.Dictionary;

        public ShipSpawnView GetSpawnPrefab(ShipType shipType)
        {
            if (SpawnShips.TryGetValue(shipType, out ShipSpawnView spawnTransform))
            {
                return spawnTransform;
            }

            return SpawnShips.Values.FirstOrDefault();
        }

        public void InvokeSpawnShipEvent(bool success)
        {
            OnSpawnShip?.Invoke(success);
        }

        public void AddReinforcement(ShipType shipType)
        {
            OnReinforcementAdded?.Invoke(shipType, factionsModel.GetFactionData(FactionType)[shipType].Icon);
        }
    }
}