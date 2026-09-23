using System;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveView
    {
        Vector3 Position { get; }
        Vector3 Forward { get; }
        Transform RootTransform { get; }
        Vector3? CurrentPathTangent { get; }
        bool IsTurningToRoute { get; }
        string ShipName { get; }
        bool LogNavigationDecisions { get; }

        void InitializePlayback();
        void SetPose(Vector3 position, Quaternion rotation);
        void PlayHyperSpace(Vector3 destination, float duration, Action completed);
        void PlayPath(ShipNavigationPlan plan, float speed, float rotationSpeed,
            float turnAcceleration, float maximumBankAngle, Action completed);
        void Face(Vector3 direction, float rotationSpeed,
            float turnAcceleration, float maximumBankAngle);
        void StopPath();
        void SetSelected(bool selected, bool moving);
        void Tick(float deltaTime);
    }
}
