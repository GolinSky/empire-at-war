using System;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Radar
{
    public interface IRadarModelObserver : IModelObserver
    {
        event Action<RaycastHit> OnHitDetected; 
        float Range { get; }
        float Delay { get; }
        LayerMask LayerMask { get; }
    }

    [Serializable]
    public class RadarModel : InnerModel, IRadarModelObserver
    {
        public event Action<RaycastHit> OnHitDetected;
        
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

        public void AddHit(RaycastHit raycastHit)
        {
            OnHitDetected?.Invoke(raycastHit);
        }
    }
}