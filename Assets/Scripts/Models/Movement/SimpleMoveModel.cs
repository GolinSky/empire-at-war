using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Utils.Random;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Models.Movement
{
    public interface ISimpleMoveModelObserver : IModelObserver
    {
        event Action<Vector3> OnTargetPositionChanged;
        Vector3 TargetPosition { get; }
        Vector3 CurrentPosition { get; }
        Vector3 FallDownDirection { get; }
        RandomVector3 FallDownRotation { get; }
        float FallDownDuration { get; }
        float Speed { get; }
        bool IsMoving { get; }
    }

    [Serializable]
    public class SimpleMoveModel : InnerModel, ISimpleMoveModelObserver
    {
        public event Action<Vector3> OnTargetPositionChanged;
      
        [SerializeField] public float speed;

        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public bool CanMove { get; private set; } = true;


        private Vector3 position;
        protected float speedCoefficient = 1;
        [Inject(Id = EntityBindType.ViewTransform)]
        protected Transform ViewTransform { get; }

        public Vector3 CurrentPosition => ViewTransform.position;
        public float Speed => speed * speedCoefficient;

        public Vector3 TargetPosition
        {
            get => position;
            set
            {
                position = value;
                OnTargetPositionChanged?.Invoke(position);
            }
        }
        public bool IsMoving => !CurrentPosition.IsEqual(TargetPosition);
    }
}