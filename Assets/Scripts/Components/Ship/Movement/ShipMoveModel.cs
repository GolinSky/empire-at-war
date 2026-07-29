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
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
    }

    public interface IShipMoveModelObserver : IDefaultMoveModelObserver
    {
        event Action OnStop;
        Vector3 HyperSpacePosition { get; }
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
        Vector3 JumpPosition { get; }
        Quaternion StartRotation { get; }
        IObservableProperty<Vector3> LookAtTargetObserver { get; }
    }

    [Serializable]
    public class ShipMoveModel : DefaultMoveModel, IShipMoveModelObserver
    {
        private const float OFFSET_HYPERSPACE_JUMP = 1000f;

        [Inject] private IShipMoveData ShipMoveData { get; }
        [Inject] private PlayerType PlayerType { get; }

        public event Action OnStop;

        public float RotationSpeed => ShipMoveData.RotationSpeed;
        public float HyperSpaceDuration => ShipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => ShipMoveData.BodyRotationMaxAngle;
        public float NavigationRadius => ShipMoveData.NavigationRadius;
        public Vector3 JumpPosition => PlayerType == PlayerType.Opponent
            ? HyperSpacePosition
            : HyperSpacePosition - Vector3.right * OFFSET_HYPERSPACE_JUMP;
        public Quaternion StartRotation => PlayerType == PlayerType.Player
            ? quaternion.identity
            : Quaternion.Euler(new Vector3(0f, -180f, 0f));
        public Vector3 HyperSpacePosition { get; set; }

        public ObservableProperty<Vector3> LookAtTarget { get; } =
            new ObservableProperty<Vector3>();

        IObservableProperty<Vector3> IShipMoveModelObserver.LookAtTargetObserver =>
            LookAtTarget;

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
