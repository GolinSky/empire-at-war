using System;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Radar
{
    public interface IRadarModelObserver : IModelObserver
    {
        float Range { get; }
        float Delay { get; }
        LayerMask LayerMask { get; }
    }

    [Serializable]
    public class RadarModel : InnerModel, IRadarModelObserver
    {
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }
        [field: SerializeField] public float Distance { get; private set; }

        [Inject]
        private PlayerType PlayerType { get; }
        
        [Inject] 
        private LayerModel LayerModel { get; }

        public LayerMask LayerMask =>
            PlayerType == PlayerType.Player ? LayerModel.PlayerLayerMask : LayerModel.EnemyLayerMask;
        
        public LayerMask EnemyLayerMask =>
            PlayerType != PlayerType.Player ? LayerModel.PlayerLayerMask : LayerModel.EnemyLayerMask;
    }
}