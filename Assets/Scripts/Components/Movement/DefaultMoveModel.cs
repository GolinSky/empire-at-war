using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models;
using EmpireAtWar.Utils.Random;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Components.Movement
{
    public interface IDefaultMoveModelObserver : IModelObserver
    {
        IObservableProperty<Vector3> TargetPositionObserver { get; }

        Vector3 StartPosition { get; }
        Vector3 CurrentPosition { get; }
        Vector3 FallDownDirection { get; }
        RandomVector3 FallDownRotation { get; }
        float FallDownDuration { get; }
        float Speed { get; }
        bool IsMoving { get; }
    }

    [Serializable]
    public class DefaultMoveModel : InnerModel, IDefaultMoveModelObserver
    {
        [SerializeField] public float speed;
        
        private Vector3 _position;
        protected float _speedCoefficient = 1;

        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public bool CanMove { get; private set; } = true;



        [Inject(Id = EntityBindType.ViewTransform)]
        public LazyInject<Transform> ViewTransform { get; }

        public Vector3 CurrentPosition => ViewTransform.Value.position;
        public float Speed => speed * _speedCoefficient;
        
        [Inject]
        public Vector3 StartPosition { get; }
        
        public ObservableProperty<Vector3> TargetPosition { get; } = new ObservableProperty<Vector3>();
        public bool IsMoving => !CurrentPosition.IsEqual(TargetPosition.Value);
        
        IObservableProperty<Vector3> IDefaultMoveModelObserver.TargetPositionObserver => TargetPosition;
    }
}