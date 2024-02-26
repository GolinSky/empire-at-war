using System;
using LightWeightFramework.Model;
using ScriptUtils.Math;
using UnityEngine;

namespace EmpireAtWar.Models.Movement
{
    public interface IMoveModelObserver:IModelObserver
    {
        event Action OnStop;
        event Action<Vector3> OnTargetPositionChanged;
        event Action<Vector3> OnHyperSpaceJump;
        Vector3 Position { get; }
        Vector3 CurrentPosition { get; }
        Vector3 HyperSpacePosition { get; }
        Vector3 FallDownDirection { get; }
        float Speed { get; }
        float RotationSpeed { get; }
        float HyperSpaceSpeed { get; }
        float MinRotationDuration { get;  }
        float FallDownDuration { get; }

        bool IsMoving { get; }
    }
    
    [Serializable]
    public class MoveModel:InnerModel, IMoveModelObserver
    {
        public event Action OnStop;
        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector3> OnHyperSpaceJump;

        [SerializeField] public float speed;
        
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float MinRotationDuration { get; private set; }
        [field: SerializeField] public float HyperSpaceSpeed { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public float Height { get; private set; }

        private Vector3 position;
        private Vector3 hyperSpacePosition;
        private float speedCoefficient = 1;

        public Vector3 CurrentPosition { get; set; }
        public float Speed => speed * speedCoefficient;
        public bool IsMoving => !CurrentPosition.IsEqual(Position);

        public Vector3 HyperSpacePosition
        {
            get => hyperSpacePosition;
            set
            {
                hyperSpacePosition = value;
                OnHyperSpaceJump?.Invoke(hyperSpacePosition);
            }
        }

        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                OnTargetPositionChanged?.Invoke(position);
            }
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            speedCoefficient = coefficient;
            OnStop?.Invoke();
        }
    }
}