using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models;
using EmpireAtWar.Utils.Random;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Components.Movement
{
    public interface IDefaultMoveData
    {
        float Speed { get; }
        float Height { get; }
        Vector3 FallDownDirection { get; }
        RandomVector3 FallDownRotation { get; }
        float FallDownDuration { get; }
        bool CanMove { get; }
    }

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
        protected float _speedCoefficient = 1;

        [Inject] protected IDefaultMoveData Data { get; }

        [Inject(Id = EntityBindType.ViewTransform)]
        public LazyInject<Transform> ViewTransform { get; }

        public Vector3 CurrentPosition => ViewTransform.Value.position;
        public float Speed => Data.Speed * _speedCoefficient;
        public float Height => Data.Height;
        public Vector3 FallDownDirection => Data.FallDownDirection;
        public RandomVector3 FallDownRotation => Data.FallDownRotation;
        public float FallDownDuration => Data.FallDownDuration;
        public bool CanMove => Data.CanMove;

        [Inject]
        public Vector3 StartPosition { get; }

        public ObservableProperty<Vector3> TargetPosition { get; } = new ObservableProperty<Vector3>();
        public bool IsMoving => TargetPosition.HasValue && !CurrentPosition.IsEqual(TargetPosition.Value);

        IObservableProperty<Vector3> IDefaultMoveModelObserver.TargetPositionObserver => TargetPosition;
    }
}
