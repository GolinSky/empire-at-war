using System;
using UnityEngine;

namespace EmpireAtWar.Models.Movement
{
    public interface IShipMoveModelObserver:ISimpleMoveModelObserver
    {
        event Action OnStop;
        event Action<Vector3> OnLookAt;
        Vector3 HyperSpacePosition { get; }
        float RotationSpeed { get; }
        float HyperSpaceSpeed { get; }
        float MinRotationDuration { get;  }
        float BodyRotationMaxAngle { get; }
    }
    
    [Serializable]
    public class ShipMoveModel:SimpleMoveModel, IShipMoveModelObserver
    {
        public event Action OnStop;
        public event Action<Vector3> OnLookAt;
        
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float MinRotationDuration { get; private set; }
        [field: SerializeField] public float HyperSpaceSpeed { get; private set; }
        [field: SerializeField] public float BodyRotationMaxAngle { get; private set; }



        public Vector3 HyperSpacePosition { get; set; }
   

        public Vector3 LookAtTarget
        {
            set => OnLookAt?.Invoke(value);
        }
        public void ApplyMoveCoefficient(float coefficient)
        {
            speedCoefficient = coefficient;
            OnStop?.Invoke();
        }
    }
}