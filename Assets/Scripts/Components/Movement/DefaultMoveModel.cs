using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Utils.Random;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Models.Movement
{
    public interface IDefaultMoveModelObserver : IModelObserver
    {
        event Action<Vector3> OnTargetPositionChanged;
        
        Vector3 StartPosition { get; }
        Vector3 TargetPosition { get; }
        Vector3 CurrentPosition { get; }
        Vector3 FallDownDirection { get; }
        RandomVector3 FallDownRotation { get; }
        float FallDownDuration { get; }
        float Speed { get; }
        bool IsMoving { get; }
        bool IsTargetPositionWasSet { get; }
    }

    [Serializable]
    public class DefaultMoveModel : InnerModel, IDefaultMoveModelObserver
    {
        public event Action<Vector3> OnTargetPositionChanged;
      
        [SerializeField] public float speed;

        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public bool CanMove { get; private set; } = true;


        private Vector3 _position;
        protected float _speedCoefficient = 1;
        [Inject(Id = EntityBindType.ViewTransform)]
        public LazyInject<Transform> ViewTransform { get; }

        public Vector3 CurrentPosition => ViewTransform.Value.position;
        public float Speed => speed * _speedCoefficient;
        public bool IsTargetPositionWasSet { get; private set; }
        
        [Inject]
        public Vector3 StartPosition { get; }

        public Vector3 TargetPosition
        {
            get => _position;
            set
            {
                _position = value;
                OnTargetPositionChanged?.Invoke(_position);
                IsTargetPositionWasSet = true;
            }
        }
        public bool IsMoving => !CurrentPosition.IsEqual(TargetPosition);
    }
}