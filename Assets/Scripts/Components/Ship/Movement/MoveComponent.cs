using System;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface IMoveComponent:IComponent
    {
        
    }
    public class MoveComponent : BaseComponent<MoveModel>, IMovable, IMoveComponent, IMoveCommand, ITickable
    {
        private readonly ICameraService cameraService;
        private Transform transform;
        public bool CanMove => true;

        public MoveComponent(IModel model, ICameraService cameraService, Vector3 startPosition) : base(model)
        {
            this.cameraService = cameraService;
            startPosition.y = Model.Height;
            Model.HyperSpacePosition = startPosition;
            Model.Position = startPosition;
        }
        
        public void MoveToPosition(Vector2 screenPosition)
        {
            Model.Position = GetWorldCoordinate(screenPosition);
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = cameraService.GetWorldPoint(screenPosition);
            point.y = Model.Height;
            return point;
        }

        public void Assign(Transform transform)
        {
            this.transform = transform;
        }
        
        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            throw new Exception();
        }

        public void Tick()
        {
            if (transform != null)
            {
                Model.CurrentPosition = transform.position;
                
            }
        }
    }
}