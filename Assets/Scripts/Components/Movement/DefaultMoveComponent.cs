using EmpireAtWar.Models.Movement;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Components.Movement
{
    public class DefaultMoveComponent: BaseComponent<DefaultMoveModel>, IDefaultMoveComponent
    {
        public bool CanMove => Model.CanMove;
        
        public DefaultMoveComponent(IModel model, Vector3 startPosition) : base(model)
        {
            Model.TargetPosition = startPosition;
        }
        
        public void MoveToPosition(Vector2 screenPosition) {}
    }
}