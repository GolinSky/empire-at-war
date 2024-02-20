using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Movement
{
    public interface IMoveModelObserver:IModelObserver
    {
        event Action<Vector3> OnTargetPositionChanged;
        event Action<Vector3> OnHyperSpaceJump;
        Vector3 Position { get; }
        Vector3 CurrentPosition { get; }
        Vector3 HyperSpacePosition { get; }
        float Speed { get; }
        float RotationSpeed { get; }
        float HyperSpaceSpeed { get; }
        float MinRotationDuration { get;  }

        bool IsMoving { get; }
    }
    
    [Serializable]
    public class MoveModel:InnerModel, IMoveModelObserver
    {
        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector3> OnHyperSpaceJump;
        
        [field: SerializeField] public float Speed { get; private set; }
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float MinRotationDuration { get; private set; }
        [field: SerializeField] public float HyperSpaceSpeed { get; private set; }
        [field: SerializeField] public float Height { get; private set; }

        private Vector3 position;
        private Vector3 hyperSpacePosition;

        public Vector3 CurrentPosition { get; set; }
        public bool IsMoving => CurrentPosition != Position;

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
    }
}