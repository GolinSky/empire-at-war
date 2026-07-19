using System;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Models;
using EmpireAtWar.Models.Factions;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveData : IDefaultMoveData
    {
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float MinRotationDuration { get; }
        float MaxRotationDuration { get; }
        float BodyRotationMaxAngle { get; }
    }

    public interface IShipMoveModelObserver:IDefaultMoveModelObserver
    {
        event Action OnStop;
        Vector3 HyperSpacePosition { get; }
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float MinRotationDuration { get; }
        float MaxRotationDuration { get; }
        float BodyRotationMaxAngle { get; }
        Vector3 JumpPosition { get; }
        Quaternion StartRotation { get; }
        IObservableProperty<Vector3> LookAtTargetObserver { get; }
    }
    
    [Serializable]
    public class ShipMoveModel:DefaultMoveModel, IShipMoveModelObserver
    {
        private const float OFFSET_HYPERSPACE_JUMP = 1000f;
        public event Action OnStop;
        
        [Inject] private IShipMoveData ShipMoveData { get; }

        public float RotationSpeed => ShipMoveData.RotationSpeed;
        public float HyperSpaceDuration => ShipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => ShipMoveData.BodyRotationMaxAngle;
        public Vector3 JumpPosition => PlayerType == PlayerType.Opponent
            ? HyperSpacePosition
            : HyperSpacePosition - Vector3.right * OFFSET_HYPERSPACE_JUMP;

        
        public Quaternion StartRotation => PlayerType == PlayerType.Player
            ? quaternion.identity
            : Quaternion.Euler(new Vector3(0, -180f, 0));


        public Vector3 HyperSpacePosition { get; set; }
   
        [Inject]
        private PlayerType PlayerType { get; }


        public float MinRotationDuration => ShipMoveData.MinRotationDuration;
        public float MaxRotationDuration => ShipMoveData.MaxRotationDuration;

        public ObservableProperty<Vector3> LookAtTarget { get; } = new ObservableProperty<Vector3>();
        IObservableProperty<Vector3> IShipMoveModelObserver.LookAtTargetObserver => LookAtTarget;


        public void ApplyMoveCoefficient(float coefficient)
        {
            _speedCoefficient = coefficient;
            OnStop?.Invoke();
        }

        public void Stop()
        {
            OnStop?.Invoke();
        }
    }
}
