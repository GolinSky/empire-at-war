using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace EmpireAtWar.Models.Radar
{
    public interface IRadarModelObserver : IModelObserver
    {
        // event Action<RaycastHit[]> OnHitDetected; 
        float Range { get; }
        float Delay { get; }
        LayerMask LayerMask { get; }
        ObservableList<IEntity> Enemies { get; }
    }

    [Serializable]
    public class RadarModel : InnerModel, IRadarModelObserver
    {
        // public event Action<RaycastHit[]> OnHitDetected;
        
        private ObservableList<IEntity> _enemies = new ObservableList<IEntity>();
        
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }
        [field: SerializeField] public float Distance { get; private set; }

        
        public ObservableList<IEntity> Enemies => _enemies;
        
        [Inject]
        private PlayerType PlayerType { get; }
        
        [Inject] 
        private LayerModel LayerModel { get; }

        public LayerMask LayerMask =>
            PlayerType == PlayerType.Player ? LayerModel.PlayerLayerMask : LayerModel.EnemyLayerMask;
        
        public LayerMask EnemyLayerMask =>
            PlayerType != PlayerType.Player ? LayerModel.PlayerLayerMask : LayerModel.EnemyLayerMask;


        // public void AddHit(RaycastHit[] raycastHits)
        // {
        //     OnHitDetected?.Invoke(raycastHits);
        // }
        
    }
}