using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveComponent : IComponent
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
        void HandleRadarContacts(IReadOnlyList<RadarContact> contacts);
        void SetMediator(IShipMovementMediator mediator);
        float HyperSpaceDuration { get; }
    }
}
