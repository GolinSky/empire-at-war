using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace EmpireAtWar.Components.Radar
{
    public interface IRadarData
    {
        float Range { get; }
        float Delay { get; }
        float Distance { get; }
    }

    public interface IRadarModelObserver : IModelObserver
    {
        // event Action<RaycastHit[]> OnHitDetected; 
        float Range { get; }
        float Delay { get; }
        LayerMask LayerMask { get; }
        ObservableList<IEntity> Enemies { get; }
    }

    [Serializable]
    public class RadarModel : PureModel, IRadarModelObserver
    {
        // public event Action<RaycastHit[]> OnHitDetected;
        
        private ObservableList<IEntity> _enemies = new ObservableList<IEntity>();
        
        [Inject] private IRadarData Data { get; }

        public float Range => Data.Range;
        public float Delay => Data.Delay;
        public float Distance => Data.Distance;

        
        public ObservableList<IEntity> Enemies => _enemies;
        
        [Inject]
        private PlayerType PlayerTypeValue { get; }
        
        [Inject] 
        private LayerModel LayerModel { get; }

        public LayerMask LayerMask =>
            PlayerTypeValue == EmpireAtWar.Models.Factions.PlayerType.Player
                ? LayerModel.PlayerLayerMask
                : LayerModel.EnemyLayerMask;
        
        public LayerMask EnemyLayerMask =>
            PlayerTypeValue != EmpireAtWar.Models.Factions.PlayerType.Player
                ? LayerModel.PlayerLayerMask
                : LayerModel.EnemyLayerMask;

        public PlayerType PlayerType => PlayerTypeValue;


        // public void AddHit(RaycastHit[] raycastHits)
        // {
        //     OnHitDetected?.Invoke(raycastHits);
        // }
        
    }
}
