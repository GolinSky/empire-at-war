using System;
using DG.Tweening;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public sealed class ShipMoveView : MonoBehaviour, IShipMoveView, IMonoComponent
    {
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;
        [SerializeField] private bool logNavigationDecisions;

        private ShipMovementTweenPlayer _tweenPlayer;

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Transform RootTransform => transform;
        public Vector3? CurrentPathTangent => _tweenPlayer.CurrentPathTangent;
        public bool IsTurningToRoute => _tweenPlayer.IsTurningToRoute;
        public string ShipName => name;
        public bool LogNavigationDecisions => logNavigationDecisions;

        public void InitializePlayback()
        {
            if (bodyTransform == null || lineRenderer == null)
            {
                throw new InvalidOperationException($"{name} has missing ship movement references.");
            }

            _tweenPlayer = new ShipMovementTweenPlayer(
                transform,
                bodyTransform,
                lineRenderer,
                hyperSpaceEase);
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void PlayHyperSpace(
            Vector3 destination,
            float duration,
            Action completed)
        {
            _tweenPlayer.PlayHyperSpace(destination, duration, completed);
        }

        public void PlayPath(
            ShipNavigationPlan plan,
            float speed,
            float rotationSpeed,
            float turnAcceleration,
            float maximumBankAngle,
            Action completed)
        {
            _tweenPlayer.PlayPath(plan, speed, rotationSpeed,
                turnAcceleration, maximumBankAngle, completed);
        }

        public void Face(
            Vector3 direction,
            float rotationSpeed,
            float turnAcceleration,
            float maximumBankAngle)
        {
            _tweenPlayer.PlayLookAt(direction, rotationSpeed,
                turnAcceleration, maximumBankAngle);
        }

        public void StopPath()
        {
            _tweenPlayer.StopPath();
        }

        public void SetSelected(bool selected, bool moving)
        {
            _tweenPlayer.SetSelected(selected, moving);
        }

        public void Tick(float deltaTime)
        {
            _tweenPlayer.Tick(deltaTime);
        }

        public void Release()
        {
            _tweenPlayer.Release();
        }
    }
}
