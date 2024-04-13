using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public interface IEnemyFactionModelObserver : IModelObserver
    {

    }

    [CreateAssetMenu(fileName = "EnemyFactionModel", menuName = "Model/EnemyFactionModel")]
    public class EnemyFactionModel : Model, IEnemyFactionModelObserver
    {
        
        [Inject] 
        private FactionsModel FactionsModel { get; }
        
        [Inject(Id = PlayerType.Opponent)]
        public FactionType FactionType { get; }

        public Dictionary<ShipType, FactionData> ShipFactionData => FactionsModel.GetShipFactionData(FactionType);
    }
}