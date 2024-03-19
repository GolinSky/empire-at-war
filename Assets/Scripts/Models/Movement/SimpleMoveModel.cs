using System;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Models.Movement
{
    public interface ISimpleMoveModelObserver : IModelObserver
    {
        event Action<Vector3> OnTargetPositionChanged;
        Vector3 Position { get; }
        Vector3 CurrentPosition { get; }
        Vector3 FallDownDirection { get; }
        float FallDownDuration { get; }
        float Speed { get; }
        bool IsMoving { get; }
    }

    [Serializable]
    public class SimpleMoveModel : InnerModel, ISimpleMoveModelObserver
    {
        public event Action<Vector3> OnTargetPositionChanged;
        
        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public bool CanMove { get; private set; } = true;

        [SerializeField] public float speed;

        private Vector3 position;

        public Vector3 CurrentPosition { get; set; }
        public float Speed => speed * speedCoefficient;
        protected float speedCoefficient = 1;

        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                OnTargetPositionChanged?.Invoke(position);
            }
        }
        public bool IsMoving => !CurrentPosition.IsEqual(Position);
    }
}