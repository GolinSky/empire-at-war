using EmpireAtWar.Components.Movement;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveComponent:IComponent
    {
        Vector3 CurrentPosition { get; }
        Transform ViewTransform { get; }
        bool IsMoving { get; }
        float MoveAround();
        Vector3 CalculateLookDirection(Vector3 targetPosition);
        void MoveToPosition(Vector3 targetPosition);
        void MoveToPositionOnScreen(Vector2 targetPosition);
        void LookAtTarget(Vector3 targetPosition);
        float GetRange(Vector3 targetPosition);
        void Stop();
        void ApplyMoveCoefficient(float coefficient);
        void HandleSelection(bool isSelected);
        float HyperSpaceDuration { get; }
        event System.Action<Vector3> TargetPositionChanged;
        event System.Action<Vector3> LookAtTargetChanged;
        event System.Action Stopped;
    }
}
