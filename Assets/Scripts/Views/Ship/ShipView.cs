using System;
using DG.Tweening;
using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace EmpireAtWar.Views.Ship
{
    public class ShipView:View<IShipModelObserver, IShipCommand>, ITickable
    {
        [SerializeField] private Canvas selectedCanvas;
        [SerializeField] private Canvas hoveredCanvas;
        [SerializeField] private RotateMode rotationMode = RotateMode.Fast; 
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        
        private Sequence moveSequence;
        protected override void OnInitialize()
        {
            transform.position = Model.StartPosition;
            HyperSpaceJump(Model.HyperSpacePosition);
            Model.OnTargetPositionChanged += UpdateTargetPosition;
            Model.OnSelected += OnSelected;
            Model.OnHyperSpaceJump += HyperSpaceJump;
            
        }

        protected override void OnDispose()
        {
            Model.OnTargetPositionChanged -= UpdateTargetPosition;
            Model.OnSelected -= OnSelected;
            Model.OnHyperSpaceJump -= HyperSpaceJump;
        }
        
        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - transform.position;
            
            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
            }

            float duration = Model.HyperSpaceSpeed;
            moveSequence = DOTween.Sequence();

            moveSequence.Append(
                transform.DORotate(
                        Quaternion.LookRotation(lookDirection).eulerAngles, 
                        1f, 
                        rotationMode)
                    .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(point, duration)
                .SetEase(hyperSpaceEase));
        }
        
        private void OnSelected(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }
        
        private void UpdateTargetPosition(Vector3 position)
        {
            Vector3 lookDirection = position - transform.position;
            
            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
            }

            var distance = Vector3.Distance(transform.position, position);
            float duration = distance / Model.Speed;
            moveSequence = DOTween.Sequence();

            moveSequence.Append(
                transform.DORotate(
                        Quaternion.LookRotation(lookDirection).eulerAngles, 
                        1f, 
                        rotationMode)
                .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(position, duration)
                .SetEase(moveEase));
        }

        private void OnMouseEnter()
        {
            hoveredCanvas.enabled = true;
        }

        private void OnMouseExit()
        {
            hoveredCanvas.enabled = false;
        }

        public void Tick()
        {
            
        }

        private void OnMouseDown()//move to view component
        {
            Command?.SelectionCommand.OnSelected();
        }
        
        public class ShipFactory:PlaceholderFactory<ShipView>
        {
        }
    }
}