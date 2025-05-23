using System;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Models;
using EmpireAtWar.Models.Factions;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveModelObserver:IDefaultMoveModelObserver
    {
        event Action OnStop;
        Vector3 HyperSpacePosition { get; }
        float RotationSpeed { get; }
        float HyperSpaceSpeed { get; }
        float MinRotationDuration { get;  }
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
        
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float MinRotationDuration { get; private set; }
        [field: SerializeField] public float HyperSpaceSpeed { get; private set; }
        [field: SerializeField] public float BodyRotationMaxAngle { get; private set; }
        public Vector3 JumpPosition => HyperSpacePosition - (PlayerType == PlayerType.Player ? Vector3.right : Vector3.left )  * OFFSET_HYPERSPACE_JUMP;

        public Quaternion StartRotation => PlayerType == PlayerType.Player
            ? quaternion.identity
            : Quaternion.Euler(new Vector3(0, -180f, 0));


        public Vector3 HyperSpacePosition { get; set; }
   
        [Inject]
        private PlayerType PlayerType { get; }

        public ObservableProperty<Vector3> LookAtTarget { get; } = new ObservableProperty<Vector3>();
        IObservableProperty<Vector3> IShipMoveModelObserver.LookAtTargetObserver => LookAtTarget;


        public void ApplyMoveCoefficient(float coefficient)
        {
            _speedCoefficient = coefficient;
            OnStop?.Invoke();
        }
    }
}